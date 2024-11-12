import subprocess
import os
import datetime
import time
import requests
from azure.storage.blob import BlobServiceClient, ContainerClient, BlobClient

########### SETTINGS ###############
capture_delay:int = 60 # number of seconds in between captures and uploads
capture_command:str = "fswebcam -d /dev/video1 -r 1280x720 img.jpg" # must name the image "img.jpg" for it to be recognized!
ping_url:str = "https://prod-17.usgovtexas.logic.azure.us:443/workflows/bd6053d466604033a7bb7a2b802d040d/triggers/manual/paths/invoke?api-version=2016-06-01&sp=%2Ftriggers%2Fmanual%2Frun&sv=1.0&sig=aBXr1J5aZn-R78z01rLsBlMG_ow8-wutoyX9s4UuDHM" # if set to a URL (a string), program will periodically make an HTTP POST call to this URL with a status update. If set to Null or blank (""), no status update ping attempt will be made.
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

def upload(data:bytes, blob_name:str = None) -> None:
    bsc:BlobServiceClient = BlobServiceClient.from_connection_string(getazblobconstr())
    cc:ContainerClient = bsc.get_container_client("cmonitor-images")
    if cc.exists() == False:
        cc.create_container()
    if blob_name == None:
        blob_name = timestamp() + ".jpg"
    bc:BlobClient = cc.get_blob_client(blob_name)
    if bc.exists():
        print("Not uploading data of length " + str(len(data)) + "! Blob already exists!")
    else:
        bc.upload_blob(data)

def status_ping(uptime_seconds:int, imgs_captured:int) -> None:
    if ping_url == None or ping_url == "":
        raise Exception("Unable to ping! Ping URL was null or empty!")
    payload = {"uptime": uptime_seconds, "captured": imgs_captured}
    headers = {"Content-Type": "application/json"}
    response = requests.post(ping_url, json=payload, headers=headers)

def monitor(upload_to_azure_blob:bool = True) -> None:
    """Infinite loop of capturing images periodically and uploading to Azure Blob Storage"""
    
    started_at:float = time.time()
    imgnum:int = 1
    while True:

        # capture
        print("Capturing image # " + str(imgnum) + "... ")
        img:bytes = capture()
        print("\tImage of " + str(len(img)) + " bytes captured!")

        # handle upload/save
        uploaded:bool = False
        if upload_to_azure_blob:
            try:
                print("\tUploading image... ")
                upload(img)
                print("\tUpload of image # " + str(imgnum) + " success!")
                uploaded = True
            except Exception as e:
                print("\tError while uploading! Msg: " + str(e))
                uploaded = False

        # if upload was unsuccessful OR uploads to azure blob is turned off
        if uploaded == False: # if it was not uploaded... either because uploading was turned off or it was turned on and it failed!
            print("\tSaving to hopper...")
            os.makedirs("./hopper", exist_ok=True) # create the hopper folder if if does not exist (exist_ok=True means it will be okay if it already exists)
            savepath = "./hopper/" + timestamp() + ".jpg" # create the full path
            si = open(savepath, "wb") # create the file in the hopper folder
            si.write(img)
            si.close()
            print("\tSaved locally to '" + savepath + "'!")

        # if they set a status ping URL, ping now
        if status_ping != None and status_ping != "":
            print("\tStatus pinging... ")
            uptime_seconds:int = int(time.time() - started_at)
            status_ping(uptime_seconds, imgnum)
    
        # wait
        imgnum = imgnum + 1
        started_waiting_at:float = time.time()
        while (time.time() - started_waiting_at) < capture_delay:
            to_wait:int = capture_delay - int(time.time() - started_waiting_at)
            print("Waiting " + str(to_wait) + " seconds until capture # " + str(imgnum) + "... ")
            time.sleep(1)

def main() -> None:

    # welcome
    print("Welcome to cmonitor! I can:")
    print()
    print("1 - Begin recording a timelapse (active monitoring), uploading these pictures to Azure Blob Storage or saving to local storage.")
    print()
    print("2 - Upload locally saved images to Azure Blob Storage")
    print()
    i:str = input("What would you like to do? > ")

    # handle choice
    if i == "1":
        
        # input
        print()
        print("Sure, I can start timelapse recording!")
        print("How would you like to handle saving of new images?")
        print()
        print("1 - Save them to Azure Blob Storage when captured, and if this fails, save to the local hopper")
        print("2 - Save images locally (to the hopper)")
        print()
        i = input("What would you like to do? > ")

        # handle
        if i == "1":
            monitor(True)
        elif i == "2":
            monitor(False)
        else:
            print("'" + i + "' was not an option!")

    elif i == "2":
        print()
        print("Checking hopper for photos...")
        if os.path.exists("./hopper"):
            files:list[str] = os.listdir("./hopper")
            if len(files) > 0: # if there are files to upload
                i = input("There are " + str(len(files)) + " in the hopper. Ready to start uploading these now? (y/n) > ")
                if i.lower() == "y":

                    # upload and delete every one
                    for i in range(len(files)):
                        file:str = files[i]

                        # Calculate percent complete
                        percent_complete:float = i / len(files)
                        percent_complete_str:str = str(round(percent_complete * 100, 1)) + "%"

                        # upload
                        f = open("./hopper/" + file, "rb")
                        data:bytes = f.read()
                        print("(" + str(i) + " / " + str(len(files)) + ", " + percent_complete_str + ") " + "Uploading '" + file + "'... ", end="")
                        upload(data, file) # upload the file (and pass the file name to it so it uses that file name, not the current time)
                        f.close() # close the file
                        print("Uploaded! ", end="")

                        # delete
                        print("Deleting... ", end="")
                        os.remove("./hopper/" + file)
                        print("Success!")

                    # after its all done, now stop
                    print("Hopper fully uploaded!")

            else: # if there were no files to upload
                print("There were no files in the hopper to upload!")
    else:
        print("'" + i + "' was not an option! Quitting...")


# Program starts below!
main()