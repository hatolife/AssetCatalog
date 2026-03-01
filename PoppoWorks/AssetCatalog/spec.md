# AssetCatalog 仕様書

## TSV形式

```
category	title	comment	url
```

- 1行目: ヘッダー
- 空行: スペーサー（余白）
- 同じcategoryは自動グループ化

## 設定

| 項目 | 説明 |
|------|------|
| width | 一覧の幅 |
| height | 一覧の高さ |
| qrCodeSize | QRコードサイズ |
| showQRCode | QRコード表示 |
| textColor | 文字色 |

## URL表示コンポーネント

| クラス | 説明 |
|-------|------|
| UrlDisplayHandler | QRクリック→URL表示 |
| UrlInputFieldController | InputField閉じる |
| UrlDisplayHandlerUdon | Udon版 |
| UrlInputFieldControllerUdon | Udon版 |

## アセンブリ構成

| asmdef | 用途 |
|--------|------|
| AssetCatalog.Runtime | ランタイム |
| AssetCatalog.Editor | エディタ |
| AssetCatalog.Udon | UdonSharp |

## ライセンス

CC0 1.0
