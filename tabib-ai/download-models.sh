#!/usr/bin/env bash
# ============================================================
# TabibAI — تحميل نماذج GGUF محلياً
# الاستخدام:
#   ./download-models.sh list        # عرض النماذج المتاحة
#   ./download-models.sh qwen3       # تحميل نموذج محدد
#   ./download-models.sh auto        # تحميل أفضل نموذج حسب RAM
#   ./download-models.sh all-small   # تحميل كل النماذج الصغيرة
# ============================================================
set -euo pipefail

MODELS_DIR="$(cd "$(dirname "$0")" && pwd)/Resources/Models"
mkdir -p "$MODELS_DIR"

declare -A MODEL_URLS=(
  [qwen3]="https://huggingface.co/Qwen/Qwen2.5-3B-Instruct-GGUF/resolve/main/qwen2.5-3b-instruct-q4_k_m.gguf"
  [llama32]="https://huggingface.co/bartowski/Llama-3.2-3B-Instruct-GGUF/resolve/main/Llama-3.2-3B-Instruct-Q4_K_M.gguf"
  [phi35]="https://huggingface.co/bartowski/Phi-3.5-mini-instruct-GGUF/resolve/main/Phi-3.5-mini-instruct-Q4_K_M.gguf"
  [qwen7]="https://huggingface.co/Qwen/Qwen2.5-7B-Instruct-GGUF/resolve/main/qwen2.5-7b-instruct-q4_k_m.gguf"
  [allam7]="https://huggingface.co/bartowski/ALLaM-AI_ALLaM-7B-Instruct-preview-GGUF/resolve/main/ALLaM-AI_ALLaM-7B-Instruct-preview-Q4_K_M.gguf"
  [jais13]="https://huggingface.co/bartowski/jais-13b-chat-GGUF/resolve/main/jais-13b-chat-Q4_K_M.gguf"
)

declare -A MODEL_SIZES_MB=(
  [qwen3]=1400 [llama32]=1400 [phi35]=1500
  [qwen7]=3500 [allam7]=3500 [jais13]=6500
)

list_models() {
  echo " النماذج المتاحة:"
  echo " ─────────────────────────────────────────────"
  for k in qwen3 llama32 phi35 qwen7 allam7 jais13; do
    printf '  %-10s %5d MB  %s\n' "$k" "${MODEL_SIZES_MB[$k]}" \
      "$([ -f "$MODELS_DIR/$k.gguf" ] && echo '✅ محمّل' || echo '—')"
  done
  echo " ─────────────────────────────────────────────"
  echo " النموذج الحالي المضمّن: gemma2-2b.gguf (1.6GB)"
}

detect_ram_gb() {
  awk '/MemTotal/ {printf "%d", $2/1024/1024}' /proc/meminfo 2>/dev/null || echo 8
}

auto_pick() {
  local ram
  ram=$(detect_ram_gb)
  if   [ "$ram" -le 4 ];  then echo qwen3
  elif [ "$ram" -le 7 ];  then echo qwen7
  else echo allam7; fi
}

download_model() {
  local key="$1"
  local url="${MODEL_URLS[$key]:-}"
  if [ -z "$url" ]; then
    echo "❌ نموذج غير معروف: $key (جرّب: ./download-models.sh list)"
    exit 1
  fi

  local out="$MODELS_DIR/$key.gguf"
  if [ -f "$out" ]; then
    echo "✅ موجود مسبقاً: $out ($(du -h "$out" | cut -f1))"
    exit 0
  fi

  local min_mb="${MODEL_SIZES_MB[$key]}"
  local min_bytes=$((min_mb * 1024 * 1024 * 9 / 10))  # 90% كحد أدنى للتحقق

  echo "⬇️  تحميل $key ..."
  echo "   من: $url"
  curl -L --progress-bar -o "$out.part" "$url"

  local actual
  actual=$(stat -c%s "$out.part")
  if [ "$actual" -lt "$min_bytes" ]; then
    echo "❌ فشل التحقق: الحجم $actual بايت (الحد الأدنى $min_bytes)"
    rm -f "$out.part"
    exit 2
  fi

  mv "$out.part" "$out"
  echo "✅ تم: $out ($(du -h "$out" | cut -f1))"
  echo "💡 أعد تشغيل TabibAI لتحميل النموذج."
}

case "${1:-list}" in
  list)      list_models ;;
  auto)      pick=$(auto_pick); echo "🤖 RAM=$(detect_ram_gb)GB → النموذج المقترح: $pick"; download_model "$pick" ;;
  all-small) for k in qwen3 llama32 phi35; do download_model "$k" || true; done ;;
  -h|--help|help) sed -n '2,9p' "$0" ;;
  *)         download_model "$1" ;;
esac
