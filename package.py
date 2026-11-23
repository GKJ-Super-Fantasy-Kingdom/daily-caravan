"""Package a Release build and check the ZIP before replacing the previous artifact."""
import json
import struct
from pathlib import Path
from tempfile import TemporaryDirectory
from xml.etree import ElementTree
from zipfile import ZIP_DEFLATED, ZipFile

root = Path(__file__).resolve().parent
listing = root / "thunderstore"
manifest = json.loads((listing / "manifest.json").read_text(encoding="utf-8"))
project = ElementTree.parse(root / "DailyCaravan.csproj")
name = manifest["name"]
version = manifest["version_number"]
if name != "DailyCaravan" or version != project.findtext(".//Version"):
    raise ValueError("Project version and manifest must match")
framework = project.findtext(".//TargetFramework")
files = {
    "LICENSE": root / "LICENSE",
    "manifest.json": listing / "manifest.json",
    "README.md": root / "README.md",
    "CHANGELOG.md": root / "CHANGELOG.md",
    "icon.png": listing / "icon.png",
    f"plugins/{name}.dll": root / "bin/Release" / framework / f"{name}.dll",
}
output = root / "dist" / f"{name}-{version}.zip"
output.parent.mkdir(exist_ok=True)
with TemporaryDirectory(dir=output.parent) as scratch:
    temp = Path(scratch) / output.name
    with ZipFile(temp, "w", ZIP_DEFLATED) as archive:
        for entry, path in files.items():
            archive.write(path, entry)
    with ZipFile(temp) as archive:
        assert archive.testzip() is None and archive.namelist() == list(files)
        assert all(archive.read(entry) == path.read_bytes() for entry, path in files.items())
        icon = archive.read("icon.png")
        assert icon[:8] == b"\x89PNG\r\n\x1a\n" and struct.unpack(">II", icon[16:24]) == (256, 256)
        assert archive.read(f"plugins/{name}.dll").startswith(b"MZ")
    temp.replace(output)
print(output)
