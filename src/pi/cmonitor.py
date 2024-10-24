import subprocess
import os
import datetime
import time
from azure.storage.blob import BlobServiceClient, ContainerClient, BlobClient

########### SETTINGS ###############
capture_delay:int = 60 # number of seconds in between captures and uploads
capture_command:str = "fswebcam -d /dev/video1 -r 1280x720 img.jpg" # must name the image "img.jpg" for it to be recognized!
####################################

def capture() -> bytes:
    """Makes a command line command to capture an image using fswebcam and returns the file content as bytes"""
    subprocess.run(capture_command, shell=True, capture_output=True, text=True) # run the command
    with open("./img.jpg", "rb") as file:
        file_content = file.read() # save the file content into memory
    os.remove("./img.jpg") # delete the file
    return file_content

def getazblobconstr() -> str:
    """Retrieves Azure Blob Storage connection string from local file."""
    f = open("../azblobconstr.txt", "rt")
    ToReturn:str = f.read()
    f.close()
    return ToReturn.replace("\n", "")

def timestamp() -> str:
    """Returns UTC date/time stamp in YYYYMMDDHHSS format."""
    return datetime.datetime.utcnow().strftime("%Y%m%d%H%M%S")

def upload(data:bytes) -> None:
    bsc:BlobServiceClient = BlobServiceClient.from_connection_string(getazblobconstr())
    cc:ContainerClient = bsc.get_container_client("cmonitor-images")
    if cc.exists() == False:
        cc.create_container()
    blob_name:str = timestamp() + ".jpg"
    bc:BlobClient = cc.get_blob_client(blob_name)
    bc.upload_blob(data)

def monitor() -> None:
    """Infinite loop of capturing images periodically and uploading to Azure Blob Storage"""
    
    imgnum:int = 1
    while True:

        # capture
        print("Capturing image # " + str(imgnum) + "... ")
        img:bytes = capture()
        print("\tImage of " + str(len(img)) + " bytes captured!")

        # upload
        upload_successful:bool = False
        try:
            t = 1 / 0
            print("\tUploading image... ")
            upload(img)
            print("\tUpload of image # " + str(imgnum) + " success!")
            upload_successful = True
        except Exception as e:
            print("\tError while uploading! Msg: " + str(e))
            upload_successful = False

        # if upload was unsuccessful, save the photo to the hopper
        if upload_successful == False:
            print("\tSaving to hopper...")
            os.makedirs("./hopper", exist_ok=True) # create the hopper folder if if does not exist (exist_ok=True means it will be okay if it already exists)
            savepath = "./hopper/" + timestamp() + ".jpg" # create the full path
            si = open(savepath, "wb") # create the file in the hopper folder
            si.write(img)
            si.close()
            print("\tSaved locally to '" + savepath + "'!")

        # wait
            imgnum = imgnum + 1
            started_waiting_at:float = time.time()
            while (time.time() - started_waiting_at) < capture_delay:
                to_wait:int = capture_delay - int(time.time() - started_waiting_at)
                print("Waiting " + str(to_wait) + " seconds until capture # " + str(imgnum) + "... ")
                time.sleep(1)

monitor()