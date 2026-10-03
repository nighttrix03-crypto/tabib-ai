# Tabib AI v1.1.0 Release Assets

## Status: ✅ Complete

### Files Generated

1. **Windows Installer** (`TabibAI-Setup.exe`)
   - Size: 123 MB
   - Location: `installer/dist/TabibAI-Setup.exe`
   - Status: ✅ Ready

2. **Windows Single-File** (`TabibAI.exe`)
   - Size: 73 MB
   - Location: `dist/TabibAI.exe`
   - Status: ✅ Ready

3. **Linux .deb Package** (`tabib-ai_1.1.0-1_amd64.deb`)
   - Size: 2.4 GB
   - Location: `tabib-ai_1.1.0-1_amd64.deb`
   - Status: ✅ Ready (contains embedded gemma2-2b model)

4. **Source ZIP** (`TabibAI-source.zip`)
   - Size: 38 KB
   - Location: `TabibAI-source.zip`
   - Status: ✅ Ready

5. **Model File** (`gemma2-2b.gguf`)
   - Size: 1.6 GB
   - Location: `release-files/gemma2-2b.gguf`
   - Status: ✅ Ready (embedded in .deb package)

6. **SHA256SUMS** (`SHA256SUMS-v1.1.0.txt`)
   - Location: `release-files/SHA256SUMS-v1.1.0.txt`
   - Status: ✅ Ready

## Build Notes

- The Linux .deb package includes the gemma2-2b.gguf model embedded inside (1.6GB)
- Windows installer contains model embedded as EmbeddedResource
- Model is copied to `deb-package/opt/tabib-ai/Models/gemma2-2b.gguf` during package build

## Next Steps

1. Push this repository to GitHub
2. Create Release tag `v1.1.0` on GitHub
3. Upload assets using provided scripts

## Scripts

- `upload_final_assets.py` - Upload all assets, replace existing ones
- `upload_missing_assets.py` - Upload only missing/updated assets
- `build_deb_with_model.sh` - Build .deb with embedded model (scripts/check/README.md)
