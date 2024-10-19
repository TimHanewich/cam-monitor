import subprocess
import os
import datetime
import time
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
    return ToReturn.replace("\n", "")

def timestamp() -> str:
    """Returns UTC date/time stamp in YYYYMMDDHHSS format."""
    return datetime.datetime.utcnow().strftime("%Y%m%d%H%M%S")

def upload(data:bytes) -> None:
    bsc:BlobServiceClient = BlobServiceClient.from_connection_string(getazblobconstr())
    cc:ContainerClient = bsc.get_container_client("cmonitor_images")
    if cc.exists() == False:
        cc.create_container()
    blob_name:str = timestamp() + ".jpg"
    bc:BlobClient = cc.get_blob_client()
    bc.upload_blob(data)

def MONITOR() -> None:
    """Infinite loop of capturing images periodically and uploading to Azure Blob Storage"""
    
    imgnum:int = 1
    delay:int = 60 # delay in between captures, in seconds
    while True:

        # capture
        print("Capturing image # " + str(imgnum) + "... ")
        img:bytes = capture()
        print("\tImage of " + str(len(img)) + " bytes captured!")

        # upload
        print("\tUploading image... ")
        upload(img)
        print("\tUpload of image # " + str(imgnum) + " success!")

        # wait
        imgnum = imgnum + 1
        started_waiting_at:float = time.time()
        while (time.time() - started_waiting_at) < delay:
            to_wait:int = int(time.time() - started_waiting_at)
            print("Waiting " + str(to_wait) + " seconds until capture # " + str(imgnum) + "... ")
            time.sleep(1)