import subprocess
import os
import datetime
import time
import requests
from azure.storage.blob import BlobServiceClient, ContainerClient, BlobClient
import subprocess
import atexit

##### SETTINGS #####
ffmpeg_cmd:str = "ffmpeg -video_size 1280x720 -i /dev/video0 -vf \"fps=0.01667\" -update 1 ./temp.jpg"   # same as above, but without the font file fully specified. For some reason, despite defaulting to using font file "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", it still seems to work without pixelation. 
azure_blob_container_name:str = "camera1"
####################

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
    cc:ContainerClient = bsc.get_container_client(azure_blob_container_name)
    if cc.exists() == False:
        cc.create_container()
    if blob_name == None:
        blob_name = timestamp() + ".jpg"
    bc:BlobClient = cc.get_blob_client(blob_name)
    if bc.exists():
        print("Not uploading data of length " + str(len(data)) + "! Blob already exists!")
    else:
        bc.upload_blob(data)

def log_error(msg:str) -> None:
    f = open("./error.txt", "a")
    utc_time:datetime.datetime = datetime.datetime.now(datetime.timezone.utc)
    utc_time_formatted:str = utc_time.strftime("%Y-%m-%d %H:%M")
    f.write(utc_time_formatted + " UTC: " + msg + "\n")
    f.close()


def main() -> None:

    # welcome
    print("Welcome to cmonitor! I can:")
    print()
    print("1 - Begin recording a timelapse (active monitoring)")
    print()
    print("2 - Upload locally saved images (in the hopper) to Azure Blob Storage")
    print()
    i:str = input("What would you like to do? > ")

    # handle choice
    if i == "1":

        # ask where to save the images
        print("Great! I can start recording...")
        print("But first, where do you want to save captured photos?")
        print()
        print("1 - Azure Blob Storage (provide your key in azblobstr.txt)")
        print()
        print("2 - Locally to the 'hopper' directory, where they can later be retrieved.")
        print()
        i = input("What do you want to do? > ")
        UploadAzureBlob:bool = False
        if i == "1":
            UploadAzureBlob = True
            print("Will save new images to Azure Blob Storage!")
        elif i == "2":
            print("Will save new images to local storage!")
        else:
            print("'" + i + "' was not an option! Quitting...")
            exit()

        print("Starting boot process...")

        # if a "./temp.jpg" file exists right NOW (before starting stream), delete it
        # the ffmpeg command will save files as "temp.jpg" to the local direcory
        # and then this program will catch it immediately and rename it to the current datetime stamp
        if os.path.exists("./temp.jpg"):
            os.remove("./temp.jpg")

        # ensure hopper folder exists
        if os.path.exists("./hopper/") == False:
            os.makedirs("./hopper/")


        # start ffmpeg streaming, saving files to "temp.jpg" in current directory
        print("Starting FFMPEG stream process...")
        global FFMPEG_STREAM_PROCESS
        FFMPEG_STREAM_PROCESS = subprocess.Popen(ffmpeg_cmd, shell=True, stdout=subprocess.DEVNULL, stderr = subprocess.DEVNULL)

        # continuously monitor
        try:
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
                    
                    # move it to hopper, also renaming it to the current datetime stamp
                    new_file_name:str = timestamp() + ".jpg"
                    new_file_path:str = "./hopper/" + new_file_name
                    os.rename("./temp.jpg", new_file_path) # rename and move to hopper
                    print("New captured frame processed and moved to hopper with name '" + new_file_name + "'!")

                    # try to upload to azure blob?
                    if UploadAzureBlob:
                        try:
                            
                            # upload
                            print("Uploading '" + new_file_name + "' now...")
                            data:bytes = open(new_file_path, "rb")
                            upload(data, new_file_name)
                            print("Uploaded!")

                            # if uploaded successfully, delete!
                            print("Deleting file '" + new_file_name + "'...")
                            os.remove(new_file_path)
                            print("Deleted!")
                        except:
                            print("Upload to Azure Blob Storage failed! It will remain in the hopper for later.")

                    imgs_captured = imgs_captured + 1

                else:
                    if image_last_captured_at == None:
                        print("@ " + str(int(time.time())) + ": No captured image detected yet!")
                    else:
                        time_elapsed:float = time.time() - image_last_captured_at
                        print(str(imgs_captured) + " images captured so far, last one " + str(int(time_elapsed)) + " seconds ago.")
                    time.sleep(1.0)
        except Exception as ex:
            msg:str = "Fatal error encountered during capture loop. Quitting now. Error encountered: " + str(ex)
            print(msg)
            log_error(msg)
            exit()
            
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