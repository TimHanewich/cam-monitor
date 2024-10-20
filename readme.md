# Cam Monitoring
A simple project that uses a USB webcam connected to a Single Board Computer (SBC) to capture images and upload them to Azure Blob Storage on a schedule.

This project has two projects within:
- **[pi](./pi/)** - Python code that runs on the SBC (an Orange Pi 3 LTS, in my case) and performs the capturing and uploading.
    - Only dependency: [azure.storage.blob](https://pypi.org/project/azure-storage-blob/).
- **[admin](./admin/)** - .NET console app for downloading photos stored in Azure Blob Storage.