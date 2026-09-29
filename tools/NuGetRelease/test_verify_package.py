import importlib.util
from pathlib import Path
import tempfile
import unittest
import warnings
import zipfile

SPEC = importlib.util.spec_from_file_location("verify_package", Path(__file__).with_name("verify-package.py"))
VERIFIER = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(VERIFIER)
RELEASE_COMMIT = "a" * 40


class PackageVerificationTests(unittest.TestCase):
    def make_package(self, folder, extra=None, dependency="", dll=True, metadata=None, groups=None):
        values = {
            "id": "BelotGameEngine", "version": "2.0.0", "license": "MIT", "license_type": "expression",
            "readme": "README.md", "repository_type": "git",
            "repository_url": "https://github.com/NikolayIT/BelotGameEngine.git", "commit": RELEASE_COMMIT,
        }
        values.update(metadata or {})
        if groups is None:
            groups = f'<group targetFramework=".NETStandard2.0">{dependency}</group>'
        package = Path(folder) / "BelotGameEngine.2.0.0.nupkg"
        with zipfile.ZipFile(package, "w") as archive:
            for name in ["_rels/.rels", "[Content_Types].xml", "README.md"]:
                archive.writestr(name, "fixture")
            if dll:
                archive.writestr("lib/netstandard2.0/Belot.Engine.dll", b"fixture")
            archive.writestr("BelotGameEngine.nuspec", f'''<package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">
              <metadata><id>{values['id']}</id><version>{values['version']}</version>
              <license type="{values['license_type']}">{values['license']}</license><readme>{values['readme']}</readme>
              <repository type="{values['repository_type']}" url="{values['repository_url']}" commit="{values['commit']}"/>
              <dependencies>{groups}</dependencies></metadata></package>''')
            if extra:
                archive.writestr(extra, b"fixture")
        return package

    def test_engine_package_is_accepted(self):
        with tempfile.TemporaryDirectory() as folder:
            VERIFIER.verify(self.make_package(folder), "2.0.0", RELEASE_COMMIT)
            self.assertEqual(VERIFIER.verify_folder(Path(folder), "2.0.0", RELEASE_COMMIT).name,
                             "BelotGameEngine.2.0.0.nupkg")

    def test_bot_assembly_weights_and_wrong_framework_are_rejected(self):
        for extra in ["lib/netstandard2.0/Belot.AI.SmartPlayer.dll", "content/weights.bin", "lib/net10.0/Belot.Engine.dll"]:
            with self.subTest(extra=extra), tempfile.TemporaryDirectory() as folder:
                with self.assertRaises(ValueError):
                    VERIFIER.verify(self.make_package(folder, extra=extra), "2.0.0", RELEASE_COMMIT)

    def test_runtime_dependency_is_rejected(self):
        with tempfile.TemporaryDirectory() as folder:
            with self.assertRaises(ValueError):
                VERIFIER.verify(self.make_package(folder, dependency='<dependency id="Belot.AI" version="1.0"/>'),
                                "2.0.0", RELEASE_COMMIT)

    def test_wrong_version_is_rejected(self):
        with tempfile.TemporaryDirectory() as folder:
            with self.assertRaises(ValueError):
                VERIFIER.verify(self.make_package(folder, metadata={"version": "1.1.0"}), "2.0.0", RELEASE_COMMIT)

    def test_missing_engine_is_rejected(self):
        with tempfile.TemporaryDirectory() as folder:
            with self.assertRaises(ValueError):
                VERIFIER.verify(self.make_package(folder, dll=False), "2.0.0", RELEASE_COMMIT)

    def test_wrong_package_identity_license_readme_and_repository_are_rejected(self):
        for metadata in [
            {"id": "AnotherPackage"}, {"license": "GPL-3.0-only"}, {"license_type": "file"},
            {"readme": "Missing.md"}, {"repository_type": "svn"},
            {"repository_url": "https://github.com/other/repo.git"},
        ]:
            with self.subTest(metadata=metadata), tempfile.TemporaryDirectory() as folder:
                with self.assertRaises(ValueError):
                    VERIFIER.verify(self.make_package(folder, metadata=metadata), "2.0.0", RELEASE_COMMIT)

    def test_missing_invalid_and_other_source_commits_are_rejected(self):
        for commit in ["", "abc", "z" * 40, "b" * 40]:
            with self.subTest(commit=commit), tempfile.TemporaryDirectory() as folder:
                with self.assertRaises(ValueError):
                    VERIFIER.verify(self.make_package(folder, metadata={"commit": commit}), "2.0.0", RELEASE_COMMIT)

    def test_expected_source_commit_must_be_full_sha(self):
        with tempfile.TemporaryDirectory() as folder:
            package = self.make_package(folder)
            for commit in ["", "abc", "z" * 40, "A" * 40]:
                with self.subTest(commit=commit), self.assertRaises(ValueError):
                    VERIFIER.verify(package, "2.0.0", commit)

    def test_missing_multiple_and_wrong_framework_groups_are_rejected(self):
        for groups in [
            "", '<group targetFramework="net10.0"/>', '<group/>',
            '<group targetFramework=".NETStandard2.0"/><group targetFramework=".NETStandard2.0"/>',
            '<group targetFramework=".NETStandard2.0"><unexpected/></group>',
        ]:
            with self.subTest(groups=groups), tempfile.TemporaryDirectory() as folder:
                with self.assertRaises(ValueError):
                    VERIFIER.verify(self.make_package(folder, groups=groups), "2.0.0", RELEASE_COMMIT)

    def test_duplicate_entries_are_rejected(self):
        with tempfile.TemporaryDirectory() as folder:
            with warnings.catch_warnings():
                warnings.simplefilter("ignore", UserWarning)
                package = self.make_package(folder, extra="README.md")
            with self.assertRaises(ValueError):
                VERIFIER.verify(package, "2.0.0", RELEASE_COMMIT)

    def test_missing_and_multiple_packages_are_rejected(self):
        with tempfile.TemporaryDirectory() as folder:
            with self.assertRaises(ValueError):
                VERIFIER.verify_folder(Path(folder), "2.0.0", RELEASE_COMMIT)
            self.make_package(folder)
            (Path(folder) / "Belot.AI.2.0.0.nupkg").write_bytes(b"fixture")
            with self.assertRaises(ValueError):
                VERIFIER.verify_folder(Path(folder), "2.0.0", RELEASE_COMMIT)


if __name__ == "__main__":
    unittest.main()
