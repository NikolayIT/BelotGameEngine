"""Reject unexpected payloads before publishing the engine-only NuGet package."""

import argparse
from pathlib import Path
import xml.etree.ElementTree as ET
import zipfile


def verify(package: Path, version: str, expected_commit: str) -> None:
    if len(expected_commit) != 40 or any(c not in "0123456789abcdef" for c in expected_commit):
        raise ValueError("Expected source commit must be a full lowercase Git SHA")
    with zipfile.ZipFile(package) as archive:
        names = archive.namelist()
        if len(names) != len(set(names)):
            raise ValueError("Duplicate archive entries")
        expected = {
            "_rels/.rels", "BelotGameEngine.nuspec", "[Content_Types].xml",
            "lib/netstandard2.0/Belot.Engine.dll", "README.md",
        }
        optional = {"lib/netstandard2.0/Belot.Engine.xml", ".signature.p7s"}
        for name in names:
            if name in expected or name in optional:
                continue
            if name.startswith("package/services/metadata/core-properties/") and name.endswith(".psmdcp"):
                continue
            raise ValueError(f"Unexpected package entry: {name}")
        if not expected.issubset(names):
            raise ValueError(f"Missing entries: {expected.difference(names)}")
        root = ET.fromstring(archive.read("BelotGameEngine.nuspec"))
        ns = {"n": root.tag.split("}")[0].lstrip("{")}
        metadata = root.find("n:metadata", ns)
        if metadata is None:
            raise ValueError("Missing package metadata")
        for key, value in (("id", "BelotGameEngine"), ("version", version), ("license", "MIT"), ("readme", "README.md")):
            if metadata.findtext(f"n:{key}", namespaces=ns) != value:
                raise ValueError(f"Unexpected {key}")
        if metadata.find("n:license", ns).get("type") != "expression":
            raise ValueError("License must be the MIT SPDX expression")
        if metadata.findall(".//n:dependency", ns):
            raise ValueError("Engine package must have no runtime package dependencies")
        dependencies = metadata.find("n:dependencies", ns)
        groups = [] if dependencies is None else dependencies.findall("n:group", ns)
        if (dependencies is None or len(dependencies) != 1 or len(groups) != 1
                or groups[0].get("targetFramework") != ".NETStandard2.0" or len(groups[0]) != 0):
            raise ValueError("Expected one empty .NETStandard2.0 dependency group")
        repository = metadata.find("n:repository", ns)
        if (repository is None or repository.get("type") != "git"
                or repository.get("url") != "https://github.com/NikolayIT/BelotGameEngine.git"):
            raise ValueError("Unexpected source repository")
        if repository.get("commit") != expected_commit:
            raise ValueError("Source commit does not match the release commit")


def verify_folder(folder: Path, version: str, expected_commit: str) -> Path:
    packages = sorted(folder.glob("*.nupkg"))
    expected = folder / f"BelotGameEngine.{version}.nupkg"
    if packages != [expected]:
        raise ValueError(f"Expected only {expected.name}, found {[p.name for p in packages]}")
    verify(expected, version, expected_commit)
    return expected


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("folder", type=Path)
    parser.add_argument("--version", required=True)
    parser.add_argument("--commit", required=True)
    args = parser.parse_args()
    expected = verify_folder(args.folder, args.version, args.commit)
    print(f"PASS: {expected.name}: engine only, netstandard2.0, MIT, README, source commit, no runtime dependencies")


if __name__ == "__main__":
    main()
