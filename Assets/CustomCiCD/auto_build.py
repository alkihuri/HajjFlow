#!/usr/bin/env python3
"""Auto build: main -> deploy, merge, Unity WebGL build into build1,
copy to repo root, commit only build files, push, return to main and update it.

Usage: python3 auto_build.py [--build-dir build1] [--unity PATH]
"""
import argparse
import os
import shutil
import subprocess
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
DEFAULT_UNITY_ROOT = Path("/Applications/Unity/Hub/Editor")


def run(*cmd, check=True, capture=False):
    print("$", " ".join(map(str, cmd)))
    res = subprocess.run(cmd, cwd=REPO, text=True, capture_output=capture)
    if check and res.returncode != 0:
        if capture:
            print(res.stdout, res.stderr)
        sys.exit(f"Command failed ({res.returncode}): {' '.join(map(str, cmd))}")
    return res.stdout.strip() if capture else res


def git(*args, **kw):
    return run("git", *args, **kw)


def find_unity(explicit):
    if explicit:
        return Path(explicit)
    version = None
    pv = REPO / "ProjectSettings" / "ProjectVersion.txt"
    for line in pv.read_text().splitlines():
        if line.startswith("m_EditorVersion:"):
            version = line.split(":", 1)[1].strip()
    path = DEFAULT_UNITY_ROOT / version / "Unity.app" / "Contents" / "MacOS" / "Unity"
    if not path.exists():
        sys.exit(f"Unity not found at {path}; use --unity")
    return path


def build(unity, build_dir):
    out = REPO / build_dir
    if out.exists():
        shutil.rmtree(out)
    run(str(unity), "-batchmode", "-nographics", "-quit",
        "-projectPath", str(REPO), "-buildTarget", "WebGL",
        "-executeMethod", "CustomCiCD.CiBuild.BuildWebGL",
        "-buildPath", build_dir, "-logFile", str(REPO / "build_log.txt"))
    if not out.exists() or not any(out.iterdir()):
        sys.exit("Build produced no output, see build_log.txt")
    return out


def copy_to_root(src):
    """Copy build contents to repo root (overwrite). Returns relative top-level paths."""
    paths = []
    for item in src.iterdir():
        dst = REPO / item.name
        if dst.is_dir() and not dst.is_symlink():
            shutil.rmtree(dst)
        elif dst.exists():
            dst.unlink()
        if item.is_dir():
            shutil.copytree(item, dst)
        else:
            shutil.copy2(item, dst)
        paths.append(item.name)
    return paths


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--build-dir", default="build1")
    ap.add_argument("--unity")
    args = ap.parse_args()

    # 1. Must be on main
    if git("branch", "--show-current", capture=True) != "main":
        sys.exit("Not on main branch, aborting.")

    # Uncommitted changes would be dragged into the merge/branch switch
    unity = find_unity(args.unity)

    # 2-3. deploy + merge
    git("checkout", "deploy")
    git("merge", "main", "--no-edit")

    try:
        # Build
        out = build(unity, args.build_dir)

        # 4. Move build to root
        paths = copy_to_root(out)
        shutil.rmtree(out)

        # 5. Commit only build-related files
        git("add", "-A", "--", *paths)
        if git("diff", "--cached", "--quiet", check=False).returncode != 0:
            git("commit", "-m", "Auto build: update WebGL build")
            # 6. Push
            git("push", "origin", "deploy")
        else:
            print("No build changes to commit.")
    finally:
        # 7-8. Back to main and update
        git("checkout", "main")
        git("pull", "--ff-only", "origin", "main", check=False)


if __name__ == "__main__":
    main()

