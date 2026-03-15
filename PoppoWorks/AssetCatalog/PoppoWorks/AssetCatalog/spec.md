# AssetCatalog 仕様書
> 更新日: 2026-03-14

この文書は配布物へ含める簡易仕様メモです。実際の運用前提は単体版 `AssetCatalog/spec.md` と同じです。

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
| textResolution | 文字描画解像度倍率 |

## URL表示コンポーネント

| クラス | 説明 |
|-------|------|
| UrlDisplayHandler | QRクリック→URL表示 |
| UrlInputFieldController | InputField閉じる |
| UrlDisplayHandlerUdon | Udon版 |
| UrlInputFieldControllerUdon | Udon版 |

## アセンブリ構成

## 補足

- QR コードは Editor 時に生成してシーンへ埋め込む
- TSV は人手編集を想定した最小構成
- UI の最終見た目は利用側プロジェクトの Canvas 構成に依存する

## ライセンス

CC0 1.0
