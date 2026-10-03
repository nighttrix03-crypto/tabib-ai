#!/usr/bin/env bash
# publish.sh — انشر Tabib AI v1.0.0 على GitHub بأمر واحد.
#
#   ./publish.sh                 # يطلب التوكن منك دون حفظه في سجل الأوامر
#   GH_TOKEN=xxx ./publish.sh    # أو مرّره كمتغير بيئة
#   ./publish.sh --dry-run       # عرض الخطة دون أي تعديل
#
# التوكن المطلوب: Fine-grained PAT على مستودع tabib-ai، صلاحية "Contents: Read and write".
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")"

if [[ -z "${GH_TOKEN:-}" && "${1:-}" != "--dry-run" ]]; then
  read -r -s -p "الصق GitHub PAT ثم اضغط Enter (لن يظهر على الشاشة): " GH_TOKEN
  echo
fi
export GH_TOKEN

echo
python3 publish-v1.0.0.py "$@"
