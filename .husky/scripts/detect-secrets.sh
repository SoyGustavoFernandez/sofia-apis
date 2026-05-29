#!/bin/sh
# ============================================================
# detect-secrets.sh — SOFIA Pre-Commit Secret Scanner
# Blocks commits containing hardcoded credentials or secrets.
# ============================================================

RED='\033[0;31m'
YELLOW='\033[1;33m'
GREEN='\033[0;32m'
NC='\033[0m' # No Color

echo "[Secret Scanner] 🔍 Scanning staged files for sensitive data..."

# --- Patterns that indicate secrets ---
PATTERNS=(
  "Password=[A-Za-z0-9@#$%^&*!_\-]{4,}"
  "pwd=[^;\"'[:space:]]{3,}"
  "[\"']?ApiKey[\"']?[[:space:]]*[:=][[:space:]]*[\"'][^\"']{8,}"
  "[\"']?api[_-]?key[\"']?[[:space:]]*[:=][[:space:]]*[\"'][^\"']{8,}"
  "[\"']?secret[\"']?[[:space:]]*[:=][[:space:]]*[\"'][^\"']{8,}"
  "token[[:space:]]*[:=][[:space:]]*[\"'][^\"']{20,}"
  "Bearer [A-Za-z0-9\-_=\.]{20,}"
  "AKIA[0-9A-Z]{16}"
  "-----BEGIN (RSA |EC |OPENSSH |DSA )?PRIVATE KEY-----"
  "AccountKey=[A-Za-z0-9+/=]{20,}"
)

# Files that are allowed to contain connection string patterns (placeholders only)
ALLOWED_FILES=(
  "appsettings.json"
  "appsettings.Development.json"
  "README.md"
  ".md"
)

# Get staged files
STAGED_FILES=$(git diff --cached --name-only --diff-filter=ACM)

if [ -z "$STAGED_FILES" ]; then
  echo "[Secret Scanner] ${GREEN}✔ No staged files to scan.${NC}"
  exit 0
fi

FOUND_SECRETS=0

for FILE in $STAGED_FILES; do
  # Skip binary files
  if git diff --cached -- "$FILE" | grep -q "Binary files"; then
    continue
  fi

  # Check if file is in the allowed list
  IS_ALLOWED=0
  for ALLOWED in "${ALLOWED_FILES[@]}"; do
    if echo "$FILE" | grep -q "$ALLOWED"; then
      IS_ALLOWED=1
      break
    fi
  done

  for PATTERN in "${PATTERNS[@]}"; do
    # Get the staged content of the file and search for pattern
    MATCHES=$(git diff --cached -U0 -- "$FILE" | grep "^+" | grep -v "^+++" | grep -iE "$PATTERN" 2>/dev/null)

    if [ -n "$MATCHES" ]; then
      if [ "$IS_ALLOWED" -eq 1 ]; then
        # Allowed file: check it's only a placeholder, not a real value
        if echo "$MATCHES" | grep -qiE "PLACEHOLDER|REDACTED|your[_-]?password|TU_PASSWORD|MiPassword|<.*>|\*\*\*"; then
          continue
        fi
      fi

      echo ""
      echo "${RED}[Secret Scanner] ❌ POTENTIAL SECRET DETECTED!${NC}"
      echo "  ${YELLOW}File   :${NC} $FILE"
      echo "  ${YELLOW}Pattern:${NC} $PATTERN"
      echo "  ${YELLOW}Match  :${NC} $(echo "$MATCHES" | head -3)"
      echo ""
      FOUND_SECRETS=1
    fi
  done
done

if [ "$FOUND_SECRETS" -eq 1 ]; then
  echo "${RED}[Secret Scanner] 🚫 COMMIT BLOCKED — Remove all secrets before committing.${NC}"
  echo ""
  echo "  💡 For development, use:"
  echo "     dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"your-connection-string\""
  echo ""
  echo "  💡 For production, use environment variables or Azure Key Vault."
  echo ""
  exit 1
fi

echo "[Secret Scanner] ${GREEN}✔ No secrets detected. Proceeding with commit.${NC}"
exit 0
