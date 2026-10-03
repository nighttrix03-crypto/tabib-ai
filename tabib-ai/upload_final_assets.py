#!/usr/bin/env python3
"""
Upload all Tabib AI v1.1.0 assets to GitHub. Uploads new assets by replacing existing ones
if they have the same name. Optimized for Windows Installer + single-file + Linux .deb + model + source.
"""

import os
import sys
import hashlib
import requests
import time
from pathlib import Path