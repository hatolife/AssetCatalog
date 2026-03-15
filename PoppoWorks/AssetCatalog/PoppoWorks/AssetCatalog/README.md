# AssetCatalog
> 更新日: 2026-03-14

VRChat 向けアセットカタログ生成ツールです。配布物へ同梱する想定で、単体 README と同じ前提を記載しています。

## 現在の位置づけ

- このディレクトリはパッケージ同梱用の複製ドキュメント
- 元資料はリポジトリ直下の `AssetCatalog/` にあります
- サンプル TSV は `AssetCatalog/sample_assets_list.tsv` を基準に利用してください

## 基本フロー

1. 空のGameObjectに `Asset Catalog Generator` コンポーネントを追加
2. TSV File欄にTSVファイルをドラッグ&ドロップ
3. 「Load」でデータを読み込み
4. 「Generate」で表示を生成

生成は Editor 上で行い、シーン内に UI と QR を配置する運用です。

## TSV形式

```
category	title	comment	url
グループ1	タイトル1	コメント1	https://example.com/1
グループ1	タイトル2	コメント2	https://example.com/2

グループ2	タイトル3	コメント3	https://example.com/3
```

- 空行を入れると余白になる
- 同じcategoryは自動グループ化

## 設定

| 項目 | 説明 |
|------|------|
| Width | 一覧の幅 |
| Height | 一覧の高さ |
| QR Code Size | QRコードのサイズ |
| Show QR Code | QRコードを表示するか |
| Text Color | テキストの色 |
| Text Resolution | テキスト解像度倍率（1-8、デフォルト4） |

## 入力データの前提

- 1 行目はヘッダー
- 空行はスペーサー扱い
- 同じ `category` は自動でまとまる
- URL の正当性チェックは別途行う前提

## 表示形式

```
■ グループ名
  タイトル (太字)
  コメント
  URL (InputField)   [QRコード]
```

- URLはInputFieldとして表示され、選択してコピーできます
- Text Resolutionを上げると文字が高解像度になります

## QRコードについて

QRコードは [goqr.me API](https://goqr.me/api/) を使用してエディタ上で生成されます。

- ランタイム負荷なし: QRコードはUnityエディタでの「Generate」実行時にAPIから取得され、テクスチャとしてシーンに埋め込まれます。VRChatワールド実行時にはAPIへのアクセスは発生しません
- API負荷軽減: QRコードは1件ずつ順次取得され、各リクエスト間に500msの待機時間を設けています
- キャッシュ: 生成されたQRコードはシーン内のImageコンポーネントにSpriteとして保持されます

※ QRコードは株式会社デンソーウェーブの登録商標です

## ライセンス

CC0 1.0 (Public Domain)
