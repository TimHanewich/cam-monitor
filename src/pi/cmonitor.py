import subprocess
import os
import datetime
import time
import requests
from azure.storage.blob import BlobServiceClient, ContainerClient, BlobClient

########### SETTINGS ###############
ping_delay:int = 60 # number of seconds that should elapse between every ping to the URL.
ping_url:str = "https://prod-17.usgovtexas.logic.azure.us:443/workflows/bd6053d466604033a7bb7a2b802d040d/triggers/manual/paths/invoke?api-version=2016-06-01&sp=%2Ftriggers%2Fmanual%2Frun&sv=1.0&sig=aBXr1J5aZn-R78z01rLsBlMG_ow8-wutoyX9s4UuDHM" # if set to a URL (a string), program will periodically make an HTTP POST call to this URL with a status update. If set to Null or blank (""), no status update ping attempt will be made.
####################################

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
        pass

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