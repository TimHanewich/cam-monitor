# Cam Monitoring
A simple project that uses a USB webcam connected to a Single Board Computer (SBC) to capture images and upload them to Azure Blob Storage on a schedule.

This project has two projects within:
- **[pi](./pi/)** - Python code that runs on the SBC (an Orange Pi 3 LTS, in my case) and performs the capturing and uploading.
    - Only dependency: [azure.storage.blob](https://pypi.org/project/azure-storage-blob/).
- **[admin](./admin/)** - .NET console app for downloading photos stored in Azure Blob Storage.

## Stitching Timelapse Together with FFMPEG
Example command:
```
ffmpeg -i %08d.jpg -r 30 output.mp4
```

The above command is described as:
- `-i %08d.jpg` specifies the input image sequence where 00000000.jpg, 00000001.jpg, etc., are the files.
- `-r 30` specificies the framerate: how many frames, or pictures, should be included in 1 second of the video. For example, if you had 30 photos and set the framerate to 30, the video would last 1 second.
- `output.mp4` (name at the end) defines the name.

## 3D-Printed Frame
I designed and printed [this frame](https://www.thingiverse.com/thing:6806473) that holds my Logitech C270 webcam against the window for a timelapse.