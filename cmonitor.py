import subprocess
import os

def capture() -> bytes:
    """Makes a command line command to capture an image using fswebcam and returns the file content as bytes"""
    cmd = "fswebcam -d /dev/video1 --no-banner -r 160x120 img.jpg"
    subprocess.run(cmd, shell=True, capture_output=True, text=True) # run the command
    with open("./img.jpg", "rb") as file:
        file_content = file.read() # save the file content into memory
    os.remove("./img.jpg") # delete the file
    return file_content