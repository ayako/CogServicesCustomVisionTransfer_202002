# Custom Vision Transfer Tool

Cognitive Services Custom Vision Project Transfer Tool

## Platform

C# console (.NET core 3.1)

## How to use

Set your Custom Vision Location and Training Key to Program.cs.

```Program.cs
        private const string cvLocation = "YOUR_CV_LOCATION";
        private const string cvTrainingKey = "YOUR_CV_TRAINING_KEY";
```

On console, type your Custom Vision project id to make copy.


## Features

### v1.1 (to be updated soon ... stay tuned!)

Enabled to make copy Custom Vision project into different subscription.
Enabled to copy 256+ images per project.
Enabled to set target iteration version to copy.

##### Limitation

- Up to 20 tags per project (due to Custom Vision limitation)

### v1.0

Able to make copy Custom Vision project into same subscription.

##### Limitation

- Up to 256 images per project
- Up to 20 tags per project
