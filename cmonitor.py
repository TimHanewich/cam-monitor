import subprocess
import os
import datetime
from azure.storage.blob import BlobServiceClient, ContainerClient, BlobBlock, BlobClient, StandardBlobTier

def capture() -> bytes:
    """Makes a command line command to capture an image using fswebcam and returns the file content as bytes"""
    cmd = "fswebcam -d /dev/video1 --no-banner -r 160x120 img.jpg"
    subprocess.run(cmd, shell=True, capture_output=True, text=True) # run the command
    with open("./img.jpg", "rb") as file:
        file_content = file.read() # save the file content into memory
    os.remove("./img.jpg") # delete the file
    return file_content

def getazblobconstr() -> str:
    """Retrieves Azure Blob Storage connection string from local file."""
    f = open("./azblobconstr.txt", "rt")
    ToReturn:str = f.read()
    f.close()
    return ToReturn

def timestamp() -> str:
    """Returns UTC date/time stamp in YYYYMMDDHHSS format."""
    return datetime.datetime.utcnow().strftime("%Y%m%d%H%M%S")