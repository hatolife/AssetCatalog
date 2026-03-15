# AssetCatalog 仕様書

この文書は `PoppoWorks/AssetCatalog/` 配下の実装に対応した簡易仕様です。利用手順よりも、データ形式、生成結果、主要コンポーネントの責務をまとめています。

## ディレクトリ構成

| パス | 内容 |
|------|------|
| `Editor/` | Inspector 拡張、TSV 処理、QR 取得 |
| `Runtime/` | 実行時コンポーネント |
| `Runtime/Data/` | `EntryGroup`, `CatalogEntry` |
| `Runtime/Udon/` | UdonSharp 用の同期・表示制御 |

## データモデル

| 型 | 主なフィールド |
|----|----------------|
| `EntryGroup` | `groupName`, `entries[]` |
| `CatalogEntry` | `entryName`, `entryNote`, `entryLink` |

`TSVHelper` は TSV の入出力を担当します。空行は `__SPACER__` として内部表現に変換されます。

## TSV 仕様

```tsv
category	title	comment	url
```

- 1 行目はヘッダー
- 2 行目以降がデータ
- 空行はスペーサー
- `category` が変わるたびに新しい `EntryGroup` を作成
- 同じ `category` 名でも非連続なら別グループ
- `\t`, `\n`, `\r` はエスケープして保存可能

## `AssetCatalogGenerator` の設定項目

| フィールド | 型 | 説明 |
|------------|----|------|
| `width` | `float` | 一覧 UI の幅 |
| `height` | `float` | 一覧 UI の高さ |
| `qrCodeSize` | `float` | QR コードサイズ |
| `showQRCode` | `bool` | QR 表示の有効化 |
| `textColor` | `Color` | テキスト色 |
| `textResolution` | `int` | 文字描画の倍率 |
| `tsvFile` | `Object` | 読み込み対象 TSV |
| `qrApiTimeoutSeconds` | `int` | QR API タイムアウト |
| `qrApiRetryCount` | `int` | QR API 再試行回数 |
| `qrApiRetryDelaySeconds` | `float` | QR API 再試行間隔 |
| `groups` | `EntryGroup[]` | 読み込み結果。Inspector では非表示 |

## 生成フロー

`CatalogGeneratorEditor` は以下の流れで UI を再生成します。

1. 対象オブジェクトが Prefab インスタンスなら unpack
2. 既存の子オブジェクトを削除
3. 直接付いている `Canvas`, `GraphicRaycaster`, `VRCUiShape` を整理
4. `CanvasRoot` と `ScrollView` 一式を生成
5. `groups` を順番に走査して見出し、スペーサー、各エントリを生成
6. QR コードが必要な行だけ PNG を取得して `Image.sprite` に反映

## 生成される階層

生成直後の基本構造は次の通りです。

```text
AssetCatalogGenerator を付けた GameObject
└── CanvasRoot
    └── ScrollView
        ├── Viewport
        │   └── Content
        │       ├── Group_{groupName}
        │       ├── Spacer
        │       └── {entryName}
        └── Scrollbar Vertical
```

`Content` には `VerticalLayoutGroup` と `ContentSizeFitter` が付き、各エントリは横並びでテキスト領域と QR 領域を持ちます。

## URL 表示関連

| クラス | 役割 |
|-------|------|
| `UrlDisplayHandler` | QR クリック時に URL を `InputField` へ表示し、コンテナを有効化 |
| `UrlInputFieldController` | URL 入力欄の開閉制御 |
| `UrlDisplayHandlerUdon` | UdonSharp 版の URL 表示制御 |
| `UrlInputFieldControllerUdon` | UdonSharp 版の入力欄制御 |

## Udon 関連

| クラス | 役割 |
|-------|------|
| `ScrollRectSyncUdon` | `ScrollRect.verticalNormalizedPosition` を同期 |

`ScrollRectSyncUdon` は所有権を取得したクライアントがスクロール位置を送信し、他クライアント側で反映します。同期周期と閾値はコンポーネント内の設定値で制御します。

## `QRImageGenerator`

単独の QR 生成用途では `QRImageGenerator` と `QRImageGeneratorEditor` を使います。

- UI 参照: `titleLabel`, `commentLabel`, `linkField`, `backgroundPanel`, `qrImage`
- 設定値: `category`, `title`, `comment`, `link`, `textColor`, `backgroundColor`
- QR API 設定: timeout, retry count, retry delay
- `Save Settings` / `Load Settings` で 1 行 TSV を保存・読込

## 補足

- QR コードは Editor 時に取得してシーンへ埋め込む運用
- Runtime 側だけでは TSV のロードは行わず、事前生成済み UI を使う前提
- 見た目は Unity UI、フォント、ワールド側の配置に依存

## ライセンス

CC0 1.0
