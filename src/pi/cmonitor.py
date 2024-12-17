import subprocess
import os
import datetime
import time
import requests
from azure.storage.blob import BlobServiceClient, ContainerClient, BlobClient
import subprocess
import atexit

########### SETTINGS ###############
ping_delay:int = 60 # number of seconds that should elapse between every ping to the URL.
ping_url:str = "https://prod-17.usgovtexas.logic.azure.us:443/workflows/bd6053d466604033a7bb7a2b802d040d/triggers/manual/paths/invoke?api-version=2016-06-01&sp=%2Ftriggers%2Fmanual%2Frun&sv=1.0&sig=aBXr1J5aZn-R78z01rLsBlMG_ow8-wutoyX9s4UuDHM" # if set to a URL (a string), program will periodically make an HTTP POST call to this URL with a status update. If set to Null or blank (""), no status update ping attempt will be made.
####################################

# Variables that must be declared at the top level, so that way they can be accessed by all functions
FFMPEG_STREAM_PROCESS:subprocess.Popen = None

def cleanup() -> None:
    """Cleans up before program is complete, killing background processes."""
    if FFMPEG_STREAM_PROCESS != None:
        # FFMPEG_STREAM_PROCESS.kill() # kill "KILLS" it, meaning SIGKILL
        print("Terminating FFMPEG process...")
        FFMPEG_STREAM_PROCESS.terminate() # terminate "terminates" it, sending it SIGTERM. Much more gentle
        FFMPEG_STREAM_PROCESS.wait() # wait for it to finish (it is terminating)
        print("FFMPEG process terminated as part of atexit.")
    else:
        print("Not terminating FFMPEG process because it was set to None!")
atexit.register(cleanup)

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
    print("1 - Begin recording a timelapse (active monitoring), saving each new image to local storage.")
    print()
    print("2 - Upload locally saved images to Azure Blob Storage")
    print()
    i:str = input("What would you like to do? > ")

    # handle choice
    if i == "1":
        print("Starting boot process...")

        # ensure hopper folder exists
        if os.path.exists("./hopper/") == False:
            os.makedirs("./hopper/")

        # if a "./temp.jpg" file exists right NOW (before starting stream), delete it
        if os.path.exists("./temp.jpg"):
            os.remove("./temp.jpg")

        # start ffmpeg streaming, saving files to "temp.jpg" in current directory
        print("Starting FFMPEG stream process...")
        #cmd:str = "ffmpeg -video_size 1280x720 -i /dev/video1 -vf fps=0.01667 -update 1 ./temp.jpg"     # the original capture command I was using
        #cmd:str = "ffmpeg -video_size 1280x720 -i /dev/video1 -vf \"fps=0.01667,drawtext=fontfile=/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf:text='%{localtime} UTC': x=10: y=10: fontcolor=white: fontsize=24: box=1: boxcolor=0x00000099\" -update 1 ./temp.jpg";    # I had tried using this command to "write" the UTC time on top, but I was getting this very bad anti-aliasing/pixelation, like in this image for example: https://i.imgur.com/EgJcf7b.jpeg. But here is a full resolution one without the text for comparison purposes: https://i.imgur.com/EHAHB6g.jpeg
        cmd:str = "ffmpeg -video_size 1280x720 -i /dev/video1 -vf \"fps=0.01667,drawtext=text='%{localtime} UTC': x=10: y=10: fontcolor=white: fontsize=24: box=1: boxcolor=0x00000099\" -update 1 ./temp.jpg"   # same as above, but without the font file fully specified. For some reason, despite defaulting to using font file "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", it still seems to work without pixelation. 
        global FFMPEG_STREAM_PROCESS
        FFMPEG_STREAM_PROCESS = subprocess.Popen(cmd, shell=True, stdout=subprocess.DEVNULL, stderr = subprocess.DEVNULL)

        # continuously monitor
        image_last_captured_at:float = None
        imgs_captured:int = 0 # count of how many images were captured overall
        while True:

            # ensure process is still running
            if FFMPEG_STREAM_PROCESS.poll() == None:
                print("FFMPEG stream confirmed to still be running.")
            else:
                print("FFMPEG stream stopped! It must have failed. Ensure FFMPEG is installed and the command used is correct.")
                exit(0)

            # check if file exists
            if os.path.exists("./temp.jpg"):
                image_last_captured_at = time.time()
                print("Image captured and detected! Processing... ")
                new_file_name:str = timestamp() + ".jpg"
                os.rename("./temp.jpg", "./hopper/" + new_file_name) # rename and move to hopper
                print("New captured frame processed and moved to hopper with name '" + new_file_name + "'!")
                imgs_captured = imgs_captured + 1
            else:
                if image_last_captured_at == None:
                    print("@ " + str(int(time.time())) + ": No captured image detected yet!")
                else:
                    time_elapsed:float = time.time() - image_last_captured_at
                    print(str(imgs_captured) + " images captured so far, last one " + str(int(time_elapsed)) + " seconds ago.")
                time.sleep(1.0)
            
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