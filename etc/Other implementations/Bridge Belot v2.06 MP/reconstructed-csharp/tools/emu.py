#!/usr/bin/env python3
"""
Unicorn-based harness that executes the ORIGINAL x86 code of belot.exe.

It maps the PE, builds the Delphi object graph the AI/rule functions expect
(cards, collections, card groups, the table, the form) and calls functions with
the Delphi register calling convention (EAX, EDX, ECX, then stack).

Recovered layouts (all address-cited in the notes):
  TAvSingleCard(0x54): +0x14 owner-player obj (0 = slot empty), +0x20 suit(0..3),
                       +0x23 hidden flag, +0x28 combined index, +0x2c rank(7..14)
  TAvCardItem:         +0x10 -> TAvSingleCard
  TList:               +4 -> array of ptrs, +8 count, +12 capacity
  TCollection:         +8 -> TList
  TAvCardGroup:        +0xc Tag(player 1..4), +0x168 -> TAvCardList collection
  TAvCardTable:        +0x168 -> GameOptions(+0x10 = trump suit),
                       +0x190(400) = led suit (4 = none),
                       played slots: idx0 +0x180, idx1 +0x174, idx2 +0x184, idx3 +0x17c
  TMainBelotForm:      +0x2e0 -> table, +0x378..+0x384 -> groups[1..4]
  globals: 0x48BEBC = contract 1..6, 0x48C074 = network mode (0 = local)
  card index: k = SUITMAP[suit]*13 + rank ,  SUITMAP = [0,1,3,2] @0x489D74
  internal suit order: 0=Clubs 1=Diamonds 2=Spades 3=Hearts
"""
import struct

import pefile
from unicorn import *
from unicorn.x86_const import *

EXE = r"C:\Dev\BelotGameEngine\etc\Other implementations\Bridge Belot v2.06 MP\belot.exe"

STACK_BASE = 0x00200000
STACK_SIZE = 0x00100000
HEAP_BASE = 0x10000000
HEAP_SIZE = 0x00400000
GDT_BASE = 0x00080000
TEB_BASE = 0x00090000
RET_MAGIC = 0x00070000
API_BASE = 0x7A000000           # synthetic addresses standing in for imported APIs
API_SLOT = 16
VIRT_BASE = 0x20000000          # arena handed out by the VirtualAlloc stub
VIRT_SIZE = 0x04000000

# stdcall argument counts for the imports the game actually reaches
API_ARGC = {
    "InitializeCriticalSection": 1, "DeleteCriticalSection": 1,
    "EnterCriticalSection": 1, "LeaveCriticalSection": 1,
    "VirtualAlloc": 4, "VirtualFree": 3, "VirtualQuery": 3,
    "LocalAlloc": 2, "LocalFree": 1, "GlobalAlloc": 2, "GlobalFree": 1,
    "GetTickCount": 0, "GetCurrentThreadId": 0, "GetLastError": 0,
    "SetLastError": 1, "GetVersion": 0, "GetThreadLocale": 0,
    "QueryPerformanceCounter": 1, "GetSystemTime": 1, "GetLocalTime": 1,
    "GetCurrentProcessId": 0, "Sleep": 1, "TlsGetValue": 1, "TlsSetValue": 2,
    "GetModuleHandleA": 1, "GetLocaleInfoA": 4, "SetThreadLocale": 1,
}

SUITMAP = [0, 1, 3, 2]          # @0x489D74 : card suit -> position used in the card index
CLUBS, DIAMONDS, SPADES, HEARTS = 0, 1, 2, 3

# contract ids
C_CLUBS, C_DIAMONDS, C_HEARTS, C_SPADES, C_NOTRUMPS, C_ALLTRUMPS = 1, 2, 3, 4, 5, 6
# trump suit (card encoding) for each suit contract
CONTRACT_TRUMP_SUIT = {C_CLUBS: CLUBS, C_DIAMONDS: DIAMONDS, C_HEARTS: HEARTS, C_SPADES: SPADES}

# Delphi runtime error raisers -> these must never be reached with valid input.
ERR_FUNCS = {0x402788: "range/error", 0x402da0: "range check", 0x402da8: "overflow",
             0x4035c0: "RaiseException"}
# Pure-GUI helpers that are irrelevant to game logic; stubbed to a plain return.
STUB_FUNCS = {0x450d80, 0x450da0, 0x4482c4, 0x44b720, 0x48242c}


class EmuError(Exception):
    pass


def _gdt_entry(base, limit, access, flags):
    if limit > 0xFFFFF:
        limit >>= 12
        flags |= 0x8
    e = limit & 0xFFFF
    e |= (base & 0xFFFFFF) << 16
    e |= (access & 0xFF) << 40
    e |= ((limit >> 16) & 0xF) << 48
    e |= (flags & 0xF) << 52
    e |= ((base >> 24) & 0xFF) << 56
    return struct.pack("<Q", e)


class BelotEmu:
    def __init__(self, trace=False):
        self.pe = pefile.PE(EXE)
        self.raw = open(EXE, "rb").read()
        self.image_base = self.pe.OPTIONAL_HEADER.ImageBase
        self.uc = Uc(UC_ARCH_X86, UC_MODE_32)
        self.trace = trace
        self.last_addrs = []
        self._map_image()
        self._map_aux()
        self._setup_fs()
        self._setup_imports()
        self._install_hooks()
        self.heap_ptr = HEAP_BASE
        self.init_runtime_tables()

    def _setup_imports(self):
        """Point every IAT slot at a synthetic address so imported APIs can be emulated."""
        self.uc.mem_map(API_BASE, 0x10000)
        self.uc.mem_map(VIRT_BASE, VIRT_SIZE)
        self.virt_ptr = VIRT_BASE
        self.api_by_addr = {}
        self.unknown_apis = set()
        i = 0
        if not hasattr(self.pe, "DIRECTORY_ENTRY_IMPORT"):
            return
        for entry in self.pe.DIRECTORY_ENTRY_IMPORT:
            dll = entry.dll.decode(errors="replace")
            for imp in entry.imports:
                name = imp.name.decode(errors="replace") if imp.name else f"ord{imp.ordinal}"
                stub = API_BASE + i * API_SLOT
                i += 1
                self.api_by_addr[stub] = (dll, name)
                if imp.address:
                    self.w32(imp.address, stub)

    def _emulate_api(self, uc, addr):
        dll, name = self.api_by_addr[addr]
        esp = uc.reg_read(UC_X86_REG_ESP)
        ret = struct.unpack("<I", uc.mem_read(esp, 4))[0]
        argc = API_ARGC.get(name)
        if argc is None:
            self.unknown_apis.add(f"{dll}!{name}")
            argc = 0
        args = [struct.unpack("<I", uc.mem_read(esp + 4 + 4 * k, 4))[0] for k in range(argc)]

        result = 1
        if name == "VirtualAlloc":
            size = (args[1] + 0xFFFF) & ~0xFFFF
            block = self.virt_ptr
            self.virt_ptr += max(size, 0x10000)
            if self.virt_ptr > VIRT_BASE + VIRT_SIZE:
                raise EmuError("VirtualAlloc arena exhausted")
            uc.mem_write(block, b"\x00" * min(size, 0x100000))
            result = args[0] if args[0] else block
            if args[0]:
                result = args[0]   # committing an already-reserved range
        elif name in ("LocalAlloc", "GlobalAlloc"):
            size = args[1] if len(args) > 1 else 0x1000
            block = self.virt_ptr
            self.virt_ptr += (size + 0xFFF) & ~0xFFF
            result = block
        elif name in ("VirtualFree", "LocalFree", "GlobalFree"):
            result = 1
        elif name == "VirtualQuery":
            result = 0        # "unknown region" — the RTL falls back to its own bookkeeping
        elif name in ("GetTickCount", "GetCurrentThreadId", "GetCurrentProcessId"):
            result = 0x1000
        elif name in ("GetLastError", "TlsGetValue"):
            result = 0
        elif name == "GetVersion":
            result = 0x0A28    # NT 5.0-ish
        uc.reg_write(UC_X86_REG_EAX, result)
        uc.reg_write(UC_X86_REG_ESP, esp + 4 + 4 * argc)   # stdcall: callee cleans
        uc.reg_write(UC_X86_REG_EIP, ret)

    # ---------- memory ----------
    def _map_image(self):
        size = (self.pe.OPTIONAL_HEADER.SizeOfImage + 0xFFF) & ~0xFFF
        self.uc.mem_map(self.image_base, max(size, 0x100000))
        for s in self.pe.sections:
            va = self.image_base + s.VirtualAddress
            data = self.raw[s.PointerToRawData:s.PointerToRawData + s.SizeOfRawData]
            if data:
                self.uc.mem_write(va, data)

    def _map_aux(self):
        self.uc.mem_map(STACK_BASE, STACK_SIZE)
        self.uc.mem_map(HEAP_BASE, HEAP_SIZE)
        self.uc.mem_map(GDT_BASE, 0x1000)
        self.uc.mem_map(TEB_BASE, 0x1000)
        self.uc.mem_map(RET_MAGIC & ~0xFFF, 0x1000)

    def _setup_fs(self):
        # Loading a GDT means every selector is resolved through it, so flat
        # descriptors are needed for CS/DS/SS/ES as well as the FS one that makes
        # `fs:[0]` (Delphi's SEH chain) land in a scratch page.
        # Ring-0 descriptors: loading SS requires DPL == CPL, and CPL stays 0 here.
        # CS is left at Unicorn's default (flat) — reloading it raises.
        self.uc.mem_write(GDT_BASE + 8 * 1, _gdt_entry(TEB_BASE, 0xFFFFFFFF, 0x92, 0xC))
        self.uc.mem_write(GDT_BASE + 8 * 3, _gdt_entry(0, 0xFFFFFFFF, 0x92, 0xC))  # data/stack
        self.uc.reg_write(UC_X86_REG_GDTR, (0, GDT_BASE, 0x1000, 0x0))
        for reg in (UC_X86_REG_DS, UC_X86_REG_ES, UC_X86_REG_SS):
            self.uc.reg_write(reg, 3 << 3)
        self.uc.reg_write(UC_X86_REG_FS, 1 << 3)
        self.uc.mem_write(TEB_BASE, b"\xff" * 4)  # fs:[0] = -1 (end of SEH chain)

    def _install_hooks(self):
        def on_code(uc, address, size, _):
            if self.trace:
                self.last_addrs.append(address)
                if len(self.last_addrs) > 64:
                    self.last_addrs.pop(0)
            if address >= API_BASE and address in self.api_by_addr:
                self._emulate_api(uc, address)
                return
            if address in ERR_FUNCS:
                raise EmuError(f"runtime error routine hit: {ERR_FUNCS[address]} @0x{address:x} "
                               f"trail={[hex(a) for a in self.last_addrs[-16:]]}")
            if address in STUB_FUNCS:
                # emulate `ret` immediately
                esp = uc.reg_read(UC_X86_REG_ESP)
                ret = struct.unpack("<I", uc.mem_read(esp, 4))[0]
                uc.reg_write(UC_X86_REG_ESP, esp + 4)
                uc.reg_write(UC_X86_REG_EIP, ret)

        def on_invalid(uc, access, address, size, value, _):
            self.fault = (access, address, size, value, uc.reg_read(UC_X86_REG_EIP),
                          list(self.last_addrs))
            return False

        self.uc.hook_add(UC_HOOK_CODE, on_code)
        self.uc.hook_add(UC_HOOK_MEM_UNMAPPED | UC_HOOK_MEM_FETCH_UNMAPPED, on_invalid)

    # ---------- heap ----------
    def alloc(self, size, zero=True):
        addr = (self.heap_ptr + 0xF) & ~0xF
        self.heap_ptr = addr + size
        if self.heap_ptr > HEAP_BASE + HEAP_SIZE:
            raise EmuError("heap exhausted")
        if zero:
            self.uc.mem_write(addr, b"\x00" * size)
        return addr

    def reset_heap(self):
        self.heap_ptr = HEAP_BASE

    def w8(self, a, v):
        self.uc.mem_write(a, bytes([v & 0xFF]))

    def w32(self, a, v):
        self.uc.mem_write(a, struct.pack("<I", v & 0xFFFFFFFF))

    def r8(self, a):
        return self.uc.mem_read(a, 1)[0]

    def r32(self, a):
        return struct.unpack("<I", self.uc.mem_read(a, 4))[0]

    # ---------- calling ----------
    def call(self, func, eax=0, edx=0, ecx=0, stack=(), timeout_insns=20_000_000):
        esp = STACK_BASE + STACK_SIZE - 0x2000
        for v in reversed(stack):
            esp -= 4
            self.uc.mem_write(esp, struct.pack("<I", v & 0xFFFFFFFF))
        esp -= 4
        self.uc.mem_write(esp, struct.pack("<I", RET_MAGIC))
        self.uc.reg_write(UC_X86_REG_ESP, esp)
        self.uc.reg_write(UC_X86_REG_EBP, STACK_BASE + STACK_SIZE - 0x1000)
        self.uc.reg_write(UC_X86_REG_EAX, eax & 0xFFFFFFFF)
        self.uc.reg_write(UC_X86_REG_EDX, edx & 0xFFFFFFFF)
        self.uc.reg_write(UC_X86_REG_ECX, ecx & 0xFFFFFFFF)
        self.last_addrs = []
        self.fault = None
        try:
            self.uc.emu_start(func, RET_MAGIC, count=timeout_insns)
        except UcError as ex:
            if self.fault:
                acc, addr, size, val, eip, trail = self.fault
                raise EmuError(
                    f"{ex}: access={acc} addr=0x{addr:x} size={size} val=0x{val:x} eip=0x{eip:x} "
                    f"trail={[hex(a) for a in trail[-12:]]}") from None
            raise EmuError(f"{ex} eip=0x{self.uc.reg_read(UC_X86_REG_EIP):x} "
                           f"trail={[hex(a) for a in self.last_addrs[-12:]]}") from None
        return self.uc.reg_read(UC_X86_REG_EAX)

    def run_range(self, start, stop, regs=None, count=50_000_000):
        """Run a raw code range (used for the table-init loops)."""
        esp = STACK_BASE + STACK_SIZE - 0x4000
        self.uc.reg_write(UC_X86_REG_ESP, esp)
        self.uc.reg_write(UC_X86_REG_EBP, esp + 0x800)
        for r, v in (regs or {}).items():
            self.uc.reg_write(r, v)
        self.uc.emu_start(start, stop, count=count)

    # ---------- runtime tables ----------
    def init_runtime_tables(self):
        """Execute the game's own strength/points cache builders (0x475799 .. 0x475949)."""
        # Two back-to-back triple loops: strength cache then points cache.
        self.run_range(0x475799, 0x475949)

    def strength(self, suit, rank, contract):
        k = SUITMAP[suit] * 13 + rank
        return self.r8(0x48BAED + 6 * k + contract)

    def points(self, suit, rank, contract):
        k = SUITMAP[suit] * 13 + rank
        return self.r8(0x48BC09 + 6 * k + contract)

    # ---------- object graph ----------
    def make_card(self, suit, rank, owner=0, hidden=0):
        c = self.alloc(0x54)
        self.w32(c + 0x14, owner)
        self.w8(c + 0x20, suit)
        self.w8(c + 0x23, hidden)
        self.w32(c + 0x28, SUITMAP[suit] * 13 + rank)
        self.w32(c + 0x2C, rank)
        self.w32(c + 0x30, SUITMAP[suit] * 13 + rank)
        return c

    def make_list(self, ptrs):
        arr = self.alloc(max(4 * len(ptrs), 4))
        for i, p in enumerate(ptrs):
            self.w32(arr + 4 * i, p)
        lst = self.alloc(0x20)
        self.w32(lst + 4, arr)
        self.w32(lst + 8, len(ptrs))
        self.w32(lst + 12, len(ptrs))
        return lst

    def make_collection(self, card_ptrs):
        items = []
        for cp in card_ptrs:
            it = self.alloc(0x18)
            self.w32(it + 0x10, cp)
            items.append(it)
        coll = self.alloc(0x20)
        self.w32(coll + 8, self.make_list(items))
        return coll

    def make_player(self, number):
        """Minimal object whose +0xc is the player number (cards point at it via +0x14)."""
        p = self.alloc(0x20)
        self.w32(p + 0xC, number)
        return p

    def make_group(self, tag, cards):
        g = self.alloc(0x180)
        self.w32(g + 0xC, tag)
        self.w32(g + 0x168, self.make_collection(cards))
        return g

    def make_table(self, led_suit, trump_suit, played):
        """played: dict slot(0..3) -> card ptr (slot order is 0,1,2,3 as FUN_00486BD0 reads)."""
        opts = self.alloc(0x40)
        self.w8(opts + 0x10, trump_suit)
        t = self.alloc(0x300)
        self.w32(t + 0x168, opts)
        self.w8(t + 0x190, led_suit)
        empty = self.make_card(0, 7, owner=0, hidden=1)
        for slot, off in ((0, 0x180), (1, 0x174), (2, 0x184), (3, 0x17C)):
            self.w32(t + off, played.get(slot, empty))
        return t

    def make_form(self, table, groups):
        f = self.alloc(0x400)
        self.w32(f + 0x2E0, table)
        for i, g in enumerate(groups, start=1):   # form+0x374+i*4  (i = 1..4)
            self.w32(f + 0x374 + 4 * i, g)
        self.w32(f + 0x2D0, groups[0])
        self.w32(f + 0x2D4, groups[1])
        self.w32(f + 0x2D8, groups[2])
        self.w32(f + 0x2DC, groups[3])
        return f

    def bid_scores(self, form):
        """Run choosegame's scoring double-loop (0x47799E..0x477E5D) for all 4 players and
        read the resulting per-player/per-contract score table at 0x48BD34+p*0x18+c*4."""
        frame = STACK_BASE + STACK_SIZE - 0x8000
        self.uc.reg_write(UC_X86_REG_ESP, frame - 0x400)
        self.uc.reg_write(UC_X86_REG_EBP, frame)
        self.w32(frame - 4, form)          # [ebp-4] = Self (the form)
        self.last_addrs = []
        self.fault = None
        try:
            self.uc.emu_start(0x47799E, 0x477E5D, count=200_000_000)
        except UcError as ex:
            if self.fault:
                acc, addr, size, val, eip, trail = self.fault
                raise EmuError(f"{ex} addr=0x{addr:x} eip=0x{eip:x} "
                               f"trail={[hex(a) for a in trail[-10:]]}") from None
            raise EmuError(f"{ex} eip=0x{self.uc.reg_read(UC_X86_REG_EIP):x}") from None
        return {p: {c: struct.unpack("<i", self.uc.mem_read(0x48BD34 + p * 0x18 + c * 4, 4))[0]
                    for c in range(1, 7)} for p in (1, 2, 3, 4)}

    def set_contract(self, contract):
        self.w8(0x48BEBC, contract)

    def set_local_mode(self):
        self.w8(0x48C074, 0)          # not networked


if __name__ == "__main__":
    e = BelotEmu()
    print("image + tables loaded")
    names = {0: "C", 1: "D", 2: "S", 3: "H"}
    print("strength cache (rank -> value) built by the ORIGINAL code:")
    for contract, label in ((C_CLUBS, "clubs"), (C_NOTRUMPS, "no-trumps"), (C_ALLTRUMPS, "all-trumps")):
        row = {r: e.strength(CLUBS, r, contract) for r in range(7, 15)}
        pts = {r: e.points(CLUBS, r, contract) for r in range(7, 15)}
        print(f"  clubs card, contract={label:10} strength={row} points={pts}")
