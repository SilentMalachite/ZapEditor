#!/bin/bash

# リソースファイルを修正するスクリプト
JA_FILE="/Users/hiro/Projetct/GitHub/Zap/ZapEditor/Resources/Strings.ja.resx"
EN_FILE="/Users/hiro/Projetct/GitHub/Zap/ZapEditor/Resources/Strings.en.resx"
ZH_FILE="/Users/hiro/Projetct/GitHub/Zap/ZapEditor/Resources/Strings.zh.resx"

# 日本語の全てのリソースキーを取得
JA_KEYS=$(grep -o '<data name="[^"]*"' "$JA_FILE" | sed 's/<data name="//' | sed 's/"//' | sort)

# 英語リソースに不足しているキーを追加
for key in $JA_KEYS; do
    if ! grep -q "name=\"$key\"" "$EN_FILE"; then
        echo "Adding missing key to English: $key"
        # キーを追加するXMLフラグメント
        xml_fragment="  <data name=\"$key\" xml:space=\"preserve\">
    <value>$key</value>
  </data>"

        # </data>と</root>の間に挿入
        sed -i '' "/<\/root>/i\\
$xml_fragment\\
" "$EN_FILE"
    fi
done

# 中国語リソースに不足しているキーを追加
for key in $JA_KEYS; do
    if ! grep -q "name=\"$key\"" "$ZH_FILE"; then
        echo "Adding missing key to Chinese: $key"
        # キーを追加するXMLフラグメント
        xml_fragment="  <data name=\"$key\" xml:space=\"preserve\">
    <value>$key</value>
  </data>"

        # </data>と</root>の間に挿入
        sed -i '' "/<\/root>/i\\
$xml_fragment\\
" "$ZH_FILE"
    fi
done

echo "Resource files have been updated with missing keys"