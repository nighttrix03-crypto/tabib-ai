#!/usr/bin/env python3
"""
Upload missing/updated Tabib AI v1.1.0 release assets to GitHub.
"""

import os
import sys
import hashlib
import requests
import time
from pathlib import Path

# Configuration
GITHUB_TOKEN = os.environ.get("GITHUB_TOKEN")
REPO_OWNER = "nighttrix03-crypto"
REPO_NAME = "tabib-ai"
RELEASE_TAG = "v1.1.0"

if not GITHUB_TOKEN:
    print("❌ GITHUB_TOKEN environment variable not set")
    sys.exit(1)

HEADERS = {
    "Authorization": f"Bearer {GITHUB_TOKEN}",
    "Accept": "application/vnd.github+json",
    "X-GitHub-Api-Version": "2022-11-28"
}

API_BASE = "https://api.github.com"

def get_release_info():
    """Get release info and assets."""
    url = f"{API_BASE}/repos/{REPO_OWNER}/{REPO_NAME}/releases/tags/{RELEASE_TAG}"
    resp = requests.get(url, headers=HEADERS)
    resp.raise_for_status()
    return resp.json()

def delete_asset(asset_id):
    """Delete an asset from the release."""
    url = f"{API_BASE}/repos/{REPO_OWNER}/{REPO_NAME}/releases/assets/{asset_id}"
    resp = requests.delete(url, headers=HEADERS)
    if resp.status_code == 204:
        print(f"   🗑️  Deleted asset ID {asset_id}")
        return True
    else:
        print(f"   ❌ Failed to delete asset {asset_id}: {resp.status_code} - {resp.text}")
        return False

def upload_asset(upload_url, file_path, content_type="application/octet-stream"):
    """Upload a single asset to the release."""
    file_name = Path(file_path).name
    file_size = Path(file_path).stat().st_size
    
    print(f"⬆️  Uploading {file_name} ({file_size / 1024 / 1024:.1f} MB)...")
    
    # Read file
    with open(file_path, "rb") as f:
        file_data = f.read()
    
    # Verify SHA256
    sha256 = hashlib.sha256(file_data).hexdigest()
    print(f"   SHA256: {sha256}")
    
    # Upload
    upload_headers = HEADERS.copy()
    upload_headers["Content-Type"] = content_type
    
    # Replace {?name,label} in upload_url
    upload_url_final = upload_url.split("{")[0] + f"?name={file_name}"
    
    start_time = time.time()
    resp = requests.post(upload_url_final, headers=upload_headers, data=file_data, timeout=7200)
    elapsed = time.time() - start_time
    
    if resp.status_code in (200, 201):
        asset = resp.json()
        print(f"   ✅ Uploaded in {elapsed:.1f}s (ID: {asset['id']}, URL: {asset['browser_download_url']})")
        return asset
    else:
        print(f"   ❌ Failed: {resp.status_code} - {resp.text}")
        resp.raise_for_status()

def main():
    print("🚀 Tabib AI v1.1.0 - Upload Missing/Updated Assets")
    print("=" * 60)
    
    # Get release info
    release = get_release_info()
    release_id = release['id']
    upload_url = release['upload_url']
    print(f"📦 Release: {release['name']} (ID: {release_id})")
    
    # List current assets
    print("\n📋 Current assets:")
    for asset in release['assets']:
        print(f"   - {asset['name']} ({asset['size'] / 1024 / 1024:.1f} MB) [ID: {asset['id']}]")
    
    # Assets to update/upload
    # 1. Replace .deb (old 33KB -> new 2.4GB)
    # 2. Add gemma2-2b.gguf (1.6GB)
    assets_to_upload = [
        {
            "local_path": "/home/mezouarsohaib/tabib-ai/tabib-ai_1.1.0-1_amd64.deb",
            "content_type": "application/vnd.debian.binary-package",
            "replace_asset_name": "tabib-ai_1.1.0-1_amd64.deb"
        },
        {
            "local_path": "/home/mezouarsohaib/tabib-ai/release-files/gemma2-2b.gguf",
            "content_type": "application/octet-stream",
            "replace_asset_name": None  # New asset
        }
    ]
    
    # Verify local files
    for asset_info in assets_to_upload:
        path = Path(asset_info["local_path"])
        if not path.exists():
            print(f"❌ File not found: {path}")
            sys.exit(1)
        print(f"✅ Local file ready: {path.name} ({path.stat().st_size / 1024 / 1024:.1f} MB)")
    
    # Delete old .deb asset if exists
    for asset in release['assets']:
        if asset['name'] == "tabib-ai_1.1.0-1_amd64.deb":
            print(f"\n🗑️  Deleting old .deb asset (ID: {asset['id']}, size: {asset['size']} bytes)...")
            delete_asset(asset['id'])
            break
    
    # Upload new assets
    uploaded = []
    for asset_info in assets_to_upload:
        try:
            asset = upload_asset(upload_url, asset_info["local_path"], asset_info["content_type"])
            uploaded.append(asset)
        except Exception as e:
            print(f"❌ Failed to upload {Path(asset_info['local_path']).name}: {e}")
            sys.exit(1)
    
    print("\n" + "=" * 60)
    print("🎉 All assets uploaded successfully!")
    print(f"📋 Release: https://github.com/{REPO_OWNER}/{REPO_NAME}/releases/tag/{RELEASE_TAG}")
    print("\n📦 Updated/Uploaded assets:")
    for asset in uploaded:
        print(f"   - {asset['name']} ({asset['size'] / 1024 / 1024:.1f} MB)")

if __name__ == "__main__":
    main()