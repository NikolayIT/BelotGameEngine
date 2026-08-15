#!/usr/bin/env python3
"""Disassemble a VA range of belot.exe (x86-32), showing x87 float ops clearly."""
import sys
import pefile
from capstone import Cs, CS_ARCH_X86, CS_MODE_32

EXE = r"C:\Dev\BelotGameEngine\etc\Other implementations\Bridge Belot v2.06 MP\belot.exe"
pe = pefile.PE(EXE)
data = open(EXE, "rb").read()
IB = pe.OPTIONAL_HEADER.ImageBase


def va_off(va):
    rva = va - IB
    for s in pe.sections:
        if s.VirtualAddress <= rva < s.VirtualAddress + max(s.Misc_VirtualSize, s.SizeOfRawData):
            return rva - s.VirtualAddress + s.PointerToRawData
    return None


def rd_dword(va):
    o = va_off(va)
    return int.from_bytes(data[o:o + 4], "little")


def rd_f8(va):
    import struct
    o = va_off(va)
    return struct.unpack("<d", data[o:o + 8])[0]


def rd_f4(va):
    import struct
    o = va_off(va)
    return struct.unpack("<f", data[o:o + 4])[0]


lo = int(sys.argv[1], 16)
hi = int(sys.argv[2], 16)
off = va_off(lo)
code = data[off:off + (hi - lo)]
md = Cs(CS_ARCH_X86, CS_MODE_32)
md.detail = False
for ins in md.disasm(code, lo):
    annot = ""
    op = ins.mnemonic
    # annotate float constant loads: fld/fmul/fdiv qword/dword [imm]
    if op[0] == 'f' and '[0x' in ins.op_str:
        import re
        m = re.search(r"\[0x([0-9a-fA-F]+)\]", ins.op_str)
        if m:
            a = int(m.group(1), 16)
            try:
                if 'qword' in ins.op_str:
                    annot = f"   ; =[double] {rd_f8(a)!r}"
                elif 'dword' in ins.op_str:
                    annot = f"   ; =[float?] {rd_f4(a)!r} / [int] {rd_dword(a)}"
            except Exception:
                pass
    if op == 'call':
        annot = "   ; -> " + ins.op_str
    print(f"{ins.address:08x}  {ins.mnemonic:8s} {ins.op_str}{annot}")
