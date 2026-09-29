#!/usr/bin/env python3
"""publish-v1.0.0.py — انشر إصدار Tabib AI v1.0.0 على GitHub.

الاستخدام:
    GH_TOKEN=github_pat_xxx python3 publish-v1.0.0.py
    python3 publish-v1.0.0.py --token-file /path/to/token.txt
    python3 publish-v1.0.0.py --dry-run          # عرض الخطة دون أي تعديل

ما الذي يفعله:
  1) يتحقق من صلاحية التوكن وصلاحية الكتابة على المستودع.
  2) يحدّث ملفات المستودع (الكود المصدري + الـ Zip المصدري) عبر Contents API.
  3) ينشئ الإصدار v1.0.0 ويرفع الأصول الكبيرة (المثبّت، Zip المصدر، SHA256SUMS).
  4) يتحقق من الأسماء والأحجام وروابط التنزيل النهائية.

التوكن المطلوب: Fine-grained PAT على مستودع tabib-ai بصلاحية
"Contents: Read and write". أضف "Workflows: Read and write" فقط إن أردت
دفع ملف .github/workflows/release.yml أيضاً.
"""

import base64
import hashlib
import json
import os
import shutil
import subprocess
import sys
import time
import urllib.error
import urllib.parse
import urllib.request

REPO = "nighttrix03-crypto/tabib-ai"
TAG = "v1.0.0"
RELEASE_NAME = "طبيب AI v1.0.0"
API = "https://api.github.com"
UPLOADS = "https://uploads.github.com"
ROOT = os.path.dirname(os.path.abspath(__file__))

# ملفات المستودع (نصية وصغيرة) => تُرفع عبر Contents API
REPO_FILES = [
    "Program.cs",
    "ReportExporter.cs",
    "HistoryStore.cs",
    "TabibAI.csproj",
    "app.manifest",
    "README.md",
    "NOTICE-HAI-DEF.txt",
    "index.html",
    "TabibAI-source.zip",
    "build-windows.ps1",
    "build-installer.ps1",
    "publish-v1.0.0.py",
    "publish.sh",
    "release-files/RELEASE-NOTES-v1.0.0.md",
    "release-files/SHA256SUMS-v1.0.0.txt",
]

# أصول الإصدار => تُرفع كمرفقات release (لا حد 100MB هنا)
ASSETS = [
    "installer/dist/TabibAI-Setup.exe",
    "dist/TabibAI.exe",
    "TabibAI-source.zip",
    "release-files/SHA256SUMS-v1.0.0.txt",
]

DRY_RUN = False


def die(msg):
    print("ERROR: " + msg, file=sys.stderr)
    sys.exit(1)


def sha256_of(path):
    h = hashlib.sha256()
    with open(path, "rb") as fh:
        for chunk in iter(lambda: fh.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()


def request(method, url, token, data=None, base=API):
    """طلب JSON إلى GitHub API. يعيد (status, obj)."""
    body = None
    headers = {
        "Authorization": "Bearer " + token,
        "Accept": "application/vnd.github+json",
        "X-GitHub-Api-Version": "2022-11-28",
        "User-Agent": "tabib-ai-publisher",
    }
    if data is not None:
        body = json.dumps(data).encode("utf-8")
        headers["Content-Type"] = "application/json"
    req = urllib.request.Request(base + url, data=body, headers=headers, method=method)
    try:
        with urllib.request.urlopen(req, timeout=300) as resp:
            raw = resp.read()
            return resp.status, (json.loads(raw) if raw else None)
    except urllib.error.HTTPError as e:
        raw = e.read()
        try:
            obj = json.loads(raw)
        except Exception:
            obj = {"message": raw.decode("utf-8", "replace")}
        return e.code, obj



def upload_asset(token, release_id, path):
    """ارفع ملفاً كبيراً كمرفق إصدار عبر uploads.github.com (streaming بـ curl)."""
    name = os.path.basename(path)
    qname = urllib.parse.quote(name)
    url = "%s/repos/%s/releases/%s/assets?name=%s" % (UPLOADS, REPO, release_id, qname)
    size = os.path.getsize(path)
    print("   uploading %s (%.1f MB) ..." % (name, size / 1048576.0))
    if shutil.which("curl"):
        cmd = [
            "curl", "-sS", "--fail-with-body", "-X", "POST",
            "-H", "Authorization: Bearer " + token,
            "-H", "Content-Type: application/octet-stream",
            "-H", "Accept: application/vnd.github+json",
            "-H", "X-GitHub-Api-Version: 2022-11-28",
            "-H", "User-Agent: tabib-ai-publisher",
            "--data-binary", "@" + path,
            url,
        ]
        proc = subprocess.run(cmd, capture_output=True, text=True)
        if proc.returncode != 0:
            die("فشل رفع %s: %s" % (name, proc.stderr.strip() or proc.stdout.strip()))
        return json.loads(proc.stdout)
    with open(path, "rb") as fh:
        blob = fh.read()
    req = urllib.request.Request(
        url, data=blob, method="POST",
        headers={
            "Authorization": "Bearer " + token,
            "Content-Type": "application/octet-stream",
            "Accept": "application/vnd.github+json",
            "X-GitHub-Api-Version": "2022-11-28",
            "User-Agent": "tabib-ai-publisher",
        },
    )
    with urllib.request.urlopen(req, timeout=900) as resp:
        return json.loads(resp.read())


def upsert_repo_file(token, relpath):
    path = os.path.join(ROOT, relpath)
    if not os.path.isfile(path):
        print("   skip (missing locally): " + relpath)
        return False
    with open(path, "rb") as fh:
        blob = fh.read()
    content = base64.b64encode(blob).decode("ascii")
    status, obj = request("GET", "/repos/%s/contents/%s" % (REPO, urllib.parse.quote(relpath)), token)
    sha = obj.get("sha") if status == 200 and isinstance(obj, dict) else None
    payload = {"message": "Update %s for %s" % (relpath, TAG), "content": content, "branch": "main"}
    if sha:
        payload["sha"] = sha
    status, obj = request("PUT", "/repos/%s/contents/%s" % (REPO, urllib.parse.quote(relpath)), token, payload)
    if status in (200, 201):
        print("   %-9s %s (%d bytes)" % ("updated" if sha else "created", relpath, len(blob)))
        return True
    msg = obj.get("message") if isinstance(obj, dict) else str(obj)
    print("   FAILED    %s -> HTTP %s: %s" % (relpath, status, msg))
    return False


def build_release_notes():
    notes_path = os.path.join(ROOT, "release-files", "RELEASE-NOTES-%s.md" % TAG)
    if os.path.isfile(notes_path):
        with open(notes_path, encoding="utf-8") as fh:
            return fh.read()
    lines = ["## طبيب AI %s" % TAG, "", "أصول الإصدار:", ""]
    for rel in ASSETS:
        p = os.path.join(ROOT, rel)
        if os.path.isfile(p):
            lines.append("- `%s` — %.1f MB — `sha256:%s`" % (
                os.path.basename(rel), os.path.getsize(p) / 1048576.0, sha256_of(p)))
    return "\n".join(lines) + "\n"


def parse_args():
    token = os.environ.get("GH_TOKEN") or os.environ.get("GITHUB_TOKEN") or ""
    dry = False
    stages = []
    asset_filter = None
    args = sys.argv[1:]
    i = 0
    while i < len(args):
        arg = args[i]
        if arg == "--dry-run":
            dry = True
        elif arg == "--token-file" and i + 1 < len(args):
            with open(args[i + 1], encoding="utf-8") as fh:
                token = fh.read().strip()
            i += 1
        elif arg == "--stage" and i + 1 < len(args):
            value = args[i + 1]
            if value not in ("files", "release", "assets"):
                die("مرحلة غير معروفة: " + value)
            stages.append(value)
            i += 1
        elif arg == "--asset" and i + 1 < len(args):
            asset_filter = args[i + 1]
            i += 1
        else:
            die("وسيط غير معروف: " + arg)
        i += 1
    if not stages:
        stages = ["files", "release", "assets"]
    return token, dry, stages, asset_filter


def main():
    global DRY_RUN
    token, DRY_RUN, stages, asset_filter = parse_args()

    print("== Tabib AI %s publisher ==" % TAG)
    print("repo: %s   dry-run: %s" % (REPO, DRY_RUN))
    if not token and not DRY_RUN:
        die("لا يوجد توكن. شغّل: GH_TOKEN=xxx python3 publish-v1.0.0.py  أو استخدم --token-file")

    # 1) التحقق من التوكن والصلاحيات
    if not DRY_RUN:
        status, user = request("GET", "/user", token)
        if status != 200:
            die("التوكن غير صالح (HTTP %s: %s)" % (status, user.get("message")))
        print("token user: %s" % user.get("login"))
        status, repo = request("GET", "/repos/%s" % REPO, token)
        if status != 200:
            die("لا يمكن الوصول إلى المستودع (HTTP %s: %s)" % (status, repo.get("message")))
        perms = repo.get("permissions", {})
        print("repo permissions: push=%s admin=%s" % (perms.get("push"), perms.get("admin")))
        if not perms.get("push"):
            die("التوكن لا يملك صلاحية الكتابة (Contents: Read and write) على هذا المستودع.")
    else:
        print("[dry-run] تخطي التحقق من التوكن")

    # 2) تحديث ملفات المستودع
    failed = []
    if "files" in stages:
        print("\n[1/4] تحديث ملفات المستودع عبر Contents API")
    for rel in (REPO_FILES if "files" in stages else []):
        if DRY_RUN:
            print("   [dry-run] PUT contents/%s" % rel)
            continue
        if not upsert_repo_file(token, rel):
            failed.append(rel)

    # 3) إنشاء الإصدار أو إعادة استخدامه
    if "release" in stages or "assets" in stages:
        print("\n[2/4] الإصدار %s" % TAG)
    release = None
    if "release" not in stages and "assets" not in stages:
        print("\n[2/4] تخطي الإصدار (مرحلة files فقط)")
    elif DRY_RUN:
        print("   [dry-run] POST /releases tag_name=%s" % TAG)
    else:
        status, existing = request("GET", "/repos/%s/releases/tags/%s" % (REPO, TAG), token)
        if status == 200:
            release = existing
            print("   الإصدار موجود مسبقاً (id=%s) — سيُستخدم" % release["id"])
        else:
            payload = {
                "tag_name": TAG,
                "target_commitish": "main",
                "name": RELEASE_NAME,
                "body": build_release_notes(),
                "draft": False,
                "prerelease": False,
            }
            status, obj = request("POST", "/repos/%s/releases" % REPO, token, payload)
            if status not in (200, 201):
                die("فشل إنشاء الإصدار (HTTP %s: %s)" % (status, obj.get("message")))
            release = obj
            print("   تم إنشاء الإصدار (id=%s) وسمه %s" % (release["id"], release["tag_name"]))

    # 4) رفع الأصول
    pending = []
    if "assets" in stages:
        pending = [a for a in ASSETS
                   if asset_filter is None or os.path.basename(a) == asset_filter]
    if pending:
        print("\n[3/4] رفع أصول الإصدار")
    existing_assets = {}
    if release:
        for a in release.get("assets", []):
            existing_assets[a["name"]] = a
    for rel in pending:
        path = os.path.join(ROOT, rel)
        name = os.path.basename(rel)
        if not os.path.isfile(path):
            print("   skip (missing locally): " + rel)
            continue
        size = os.path.getsize(path)
        if DRY_RUN:
            print("   [dry-run] upload asset %s (%.1f MB)" % (name, size / 1048576.0))
            continue
        if name in existing_assets:
            aid = existing_assets[name]["id"]
            status, _ = request("DELETE", "/repos/%s/releases/assets/%s" % (REPO, aid), token)
            print("   replaced existing asset %s (delete HTTP %s)" % (name, status))
        try:
            asset = upload_asset(token, release["id"], path)
        except Exception as exc:  # noqa: BLE001
            print("   FAILED    %s -> %s" % (name, exc))
            continue
        print("   uploaded  %s (%.1f MB) state=%s" % (
            asset.get("name"), asset.get("size", 0) / 1048576.0, asset.get("state")))

    # 5) التحقق النهائي
    if stages == ["files"]:
        print("\nتم تحديث ملفات المستودع فقط (مرحلة files).")
        return
    print("\n[4/4] التحقق")
    if DRY_RUN:
        print("   [dry-run] تخطي التحقق")
        print("\n(dry-run) لا تغييرات نُفّذت.")
        return

    status, final = request("GET", "/repos/%s/releases/tags/%s" % (REPO, TAG), token)
    if status != 200:
        die("تعذر قراءة الإصدار بعد الرفع (HTTP %s)" % status)
    print("   release: %s (draft=%s)" % (final.get("html_url"), final.get("draft")))
    ok = True
    for asset in final.get("assets", []):
        local = os.path.join(ROOT, "release-files", asset["name"])
        for rel in ASSETS:
            if os.path.basename(rel) == asset["name"]:
                local = os.path.join(ROOT, rel)
        same = os.path.isfile(local) and os.path.getsize(local) == asset["size"]
        head = ""
        try:
            req = urllib.request.Request(asset["browser_download_url"], method="HEAD",
                                         headers={"User-Agent": "tabib-ai-publisher"})
            with urllib.request.urlopen(req, timeout=60) as resp:
                head = "HTTP %s, %s bytes" % (resp.status, resp.headers.get("Content-Length"))
        except Exception as exc:  # noqa: BLE001
            head = "HEAD failed: %s" % exc
            ok = False
        flag = "OK " if same else "SIZE-MISMATCH"
        if not same:
            ok = False
        print("   [%s] %-26s %12d bytes  %s" % (flag, asset["name"], asset["size"], head))
        print("        %s" % asset["browser_download_url"])

    if failed:
        ok = False
        print("\n   ملفات فشل تحديثها في المستودع: " + ", ".join(failed))
    print("\nالنتيجة: %s" % ("تم النشر والتحقق بنجاح ✅" if ok else "اكتمل مع ملاحظات ⚠️ — راجع الأعلى"))
    print("صفحة الإصدار: %s" % final.get("html_url"))
    print("الموقع: https://nighttrix03-crypto.github.io/tabib-ai/")


if __name__ == "__main__":
    main()
