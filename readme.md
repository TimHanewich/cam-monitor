# Cam Monitoring
A simple project that uses a USB webcam connected to a Single Board Computer (SBC) to capture images and upload them to Azure Blob Storage on a schedule.

This project has two projects within:
- **[pi](./pi/)** - Python code that runs on the SBC (an Orange Pi 3 LTS, in my case) and performs the capturing and uploading.
    - Only dependency: [azure.storage.blob](https://pypi.org/project/azure-storage-blob/).
- **[admin](./admin/)** - .NET console app for downloading photos stored in Azure Blob Storage.

## How to use this
I have provided all the resources here to facilitate the capturing, uploading, accessing, and stitching of images into a timelapse.

- On a SBC like a Raspberry Pi (I am using an Orange Pi 3 LTS), plug in a simple USB webcam.
- Run [cmonitor.py](./src/pi/cmonitor.py) and go through the prompts to begin recording a timelapse. This will continuously capture photos and then upload them to your Azure Blob Storage account. *But before doing so, add your Azure Blob Storage connection string, ensure the FFMPEG command it is using it capture works, etc.*
- After a while of it capturing photos, run [the admin .NET console app](./src/admin/) to download images for a certain date range. This will download the images and then rename them so they follow a standard sequence (i.e. `000001.jpg`, `000002.jpg`, etc.)
- Use [FFMPEG](https://www.ffmpeg.org/) as described below to then generate a timelapse .MP4 of those images.

## Step-by-Step for setting up new SBC
- Install software
    - `sudo apt update` and `sudo apt upgrade`
    - *(optional)* Install tmux: `sudo apt install tmux`
    - Install git: `sudo apt install git`
    - Install Python and pip: `sudo apt install python3` and `sudo apt install python3-pip`
    - Install ffmpeg: `sudo apt install ffmpeg` 
    - Install Azure Blob Storage Python package with pip: `python3 -m pip install azure-storage-blob`
    - Install Azure Identity with pip: `python3 -m pip install azure-identity`
    - *Possibly needed, I needed to do this on a Raspberry Pi Zero W deployment... I think the azure-storage-blob package relies on it:* `sudo apt install libssl1.1`
- Plug in the USB webcam.
- Git clone this repo.
- Add your Azure Blob Storage connection string to [azblobconstr.txt](./src/azblobconstr.txt).
- Run [cmonitor.py](./src/pi/cmonitor.py)!
    - Before you do, change any pertinent settings in cmonitor.py... i.e. container name, the fswebcam command.

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
I designed two frames that allow for a Logitech C270 webcam to be mounted to a window, facing outward.
- [This frame design](https://www.thingiverse.com/thing:6859186) requires you to remove the webcam from its plastic housing, but is very secure. This is recommended.
- [This frame design](https://www.thingiverse.com/thing:6806473) allows for the webcam to be mounted in its original housing. I don't recommend using this as it is not that secure, but this can be used if you really do not want to remove  the webcam from its origial housing.

## Azure Blob Storage Cost Calculator
I made [this spreadsheet](https://github.com/TimHanewich/cam-monitor/releases/download/1/azure-calculator.xlsx) to estimate the cost of uploading, storing, and accessing these images in Azure Blob Storage.

## Keeping Network Connection Active by Disabling Power Management
Run `sudo iwconfig wlan0 power off` to disable power management your SBC.

You can also run `iwconfig` which will show you, for wlan0, if Power Management is turned on or off.

[This article](https://www.dzombak.com/blog/2023/12/maintaining-a-solid-wifi-connection-on-raspberry-pi/) looks great too.

## Notable Commits
- `d975340594f16072ccf5e394e707d44f4d240ad8` - last commit before moving to ffmpeg streaming technique.