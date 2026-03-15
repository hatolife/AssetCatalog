# AssetCatalog

VRChat 向けのアセット一覧 UI 生成ツールです。`tsv` で管理したデータを Unity Editor 上で読み込み、ワールド空間 UI と URL 表示、QR コード画像をまとめて生成します。

## 構成

- `Editor/`: インスペクタ拡張、TSV 読み込み、QR コード生成
- `Runtime/`: 生成対象コンポーネント本体
- `Runtime/Data/`: カタログ用データ型
- `Runtime/Udon/`: UdonSharp 連携用コンポーネント
- サンプル入力: [sample_assets_list.tsv](/home/user/work/AssetCatalog/PoppoWorks/AssetCatalog/sample_assets_list.tsv)

## 主な使い方

1. シーン内の空の GameObject に `Asset Catalog/Asset Catalog Generator` を追加
2. `TSV File` に `.tsv` アセットを割り当てる
3. 必要なら `Open With Default App` で元ファイルを開いて編集する
4. `Load` で TSV を読み込み、インスペクタ上で内容を確認する
5. `Generate` を実行して UI を生成する

生成処理は Editor 実行前提です。ワールド実行中に TSV を読み込む仕組みではありません。

## 生成されるもの

- ルート直下に `CanvasRoot` を作成
- その配下に `ScrollView`、`Viewport`、`Content`、縦スクロールバーを構築
- グループ見出しごとに `■ グループ名` を生成
- 各エントリにタイトル、コメント、URL 表示欄、必要なら QR コードを生成

既存の子オブジェクトは `Generate` 実行時に一度消去されます。Prefab インスタンス上で実行した場合は、必要に応じて自動で unpack されます。

## TSV 形式

ヘッダーは固定で、以下の 4 列を使います。

```tsv
category	title	comment	url
グループ1	タイトル1	コメント1	https://example.com/1
グループ1	タイトル2	コメント2	https://example.com/2

グループ2	タイトル3	コメント3	https://example.com/3
```

- 1 行目は必ずヘッダー
- 空行はスペーサーとして扱われます
- `category` は連続している行単位で 1 グループになります
- 同じ `category` が離れた位置に再登場した場合は別グループとして扱われます
- `\t`, `\n`, `\r` はエスケープ文字として保存できます
- `url` が空なら URL 表示欄と QR コードは生成されません

## `Asset Catalog Generator` の設定

| 項目 | 説明 |
|------|------|
| `Width` | 生成する一覧 UI の幅 |
| `Height` | 生成する一覧 UI の高さ |
| `QR Code Size` | QR コード画像の表示サイズ |
| `Show QR Code` | URL がある項目に QR コードを表示するか |
| `Text Color` | 見出し、タイトル、コメント、URL の文字色 |
| `Text Resolution` | 文字サイズ計算に使う解像度倍率 |
| `TSV File` | 読み込み対象の `.tsv` アセット |
| `QR API Timeout (sec)` | QR API への 1 回の待機時間 |
| `QR API Retry Count` | 失敗時の再試行回数 |
| `QR API Delay (sec)` | 再試行までの待機秒数 |

## URL と QR コード

QR コードは [goqr.me API](https://goqr.me/api/) を使って Editor 上で PNG を取得し、生成した UI に Sprite として反映します。

- ランタイム中に QR API へアクセスしません
- 取得結果は URL 単位でキャッシュされます
- タイムアウト、再試行回数、再試行間隔はインスペクタから調整できます
- URL クリック時の表示切り替えは `UrlDisplayHandler` と `UrlInputFieldController` が担当します

## 単体 QR 生成

`QRImageGenerator` を使うと、1 件分のタイトル、コメント、URL、背景色を個別に設定して QR 画像だけ生成できます。

- `Save Settings`: 現在の入力内容を TSV 形式で保存
- `Load Settings`: 保存済み TSV から再読込
- `Generate QR Code`: テキスト反映と QR 画像生成を実行

## ライセンス

CC0 1.0 (Public Domain)
