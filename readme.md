# Custom Vision Transfer Tool

**Cognitive Services Custom Vision Project Transfer Tool**

**Cognitive Services Custom Vision プロジェクト複製ツール**

Custom Vision のプロジェクト作成時の学習データ(画像)なしでプロジェクトを複製するためのツールです。(他サブスクリプションへのコピーにも対応。)

## Platform

C# console (.NET core 3.1)

## How to use

On console, 

- Type your Custom Vision Training Key and Endpoint of original project.
- Type type your Custom Vision project id to make copy.

After showing project details on console,

- Type your Custom Vision Training Key and Endpoint of destination.


## Features

### v1.2

- Updated Custom Vision Training SDK to v2.0.0 (Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training 2.0.0).
- Shows simple error message to console.
- Subscription Key varidation. (Shows error message)

### v1.1

- Enabled to make copy Custom Vision project into different subscription.
- Enabled to set target iteration version to copy.
- Enabled to copy 256+ images per project. 

#### Limitation

- Up to 20 tags per project (due to Custom Vision project limitation)


### v1.0

Able to make copy Custom Vision project into same subscription.

#### Limitation

- Up to 256 images per project
- Up to 20 tags per project
