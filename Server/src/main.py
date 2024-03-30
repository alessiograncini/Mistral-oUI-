from PIL import Image
import numpy as np
from fastapi import FastAPI, File, UploadFile
from fastapi.responses import JSONResponse
from typing import List
import uvicorn
import io
import os
import shutil
import requests
import threading
from uuid import uuid4
import json


from local_test import answer_question, init_model
from detect import predict_object
app = FastAPI()


def send_answer(id: str, caption: str, latest_image: Image.Image):
    print(f"Sending caption for ID {id}:", caption)

    bytes_buffer = io.BytesIO()
    latest_image.save(bytes_buffer, format='JPEG')
    bytes_buffer.seek(0)

    requests.post(
        "http://localhost:3000/newTick",
        files={"image": ('image.jpeg', bytes_buffer)},
        data={"caption": caption, "id": id}
    )
    # print("Response ID:", response.json().get("id"))
    # time.sleep(5)


def detect_object(id: str, img: Image.Image):
    print("Detecting object")
    img_np = np.array(img)
    print("Converted image")
    (detected_objects, annotated_image) = predict_object(img_np)
    print("Predicted objects")

    new_img = Image.fromarray(annotated_image)

    image_bytes = io.BytesIO()
    new_img.save(image_bytes, format='JPEG')
    image_bytes.seek(0)

    objects = [{"name": obj[0], "xywh": list(
        obj[1])} for obj in detected_objects]

    detected_objects_json = json.dumps({"detected_objects": objects})

    print("Sending object detection")

    response = requests.post(
        "http://localhost:3000/objectDetect",
        files={"image": ('image.jpeg', image_bytes)},
        # Include detected_objects in the JSON body
        data={
            "id": id,  # Send the id as form data
            # Send the serialized JSON as a form field
            "detected_objects": detected_objects_json,
        }
    )
    print(f"Response Status: {response.status_code}")
    print("Done")


@app.post("/upload-image/")
async def create_upload_file(file: UploadFile = File(...)):
    print(f"Uploading file: {file.filename}")

    temp_dir = "temp"
    os.makedirs(temp_dir, exist_ok=True)
    temp_file_path = os.path.join(temp_dir, file.filename)

    with open(temp_file_path, "wb") as buffer:
        shutil.copyfileobj(file.file, buffer)

    uuid = str(uuid4())

    img = Image.open(temp_file_path)
    # Assuming 'answer_question' is properly defined and imported
    buffer = ""
    for text in answer_question(img, "What do you see? Keep it brief and articulate, comment specifically on things that look in motion or have likely just changed, or on things the user is looking at. Assume the image is from the user's pov."):
        if len(text) > 0:
            buffer = text
    answer = buffer
    # Clean up the temporary file
    os.remove(temp_file_path)
    from concurrent.futures import ThreadPoolExecutor

    with ThreadPoolExecutor(max_workers=2) as executor:
        executor.submit(send_answer, uuid, answer, img)
        executor.submit(detect_object, uuid, img)

    return JSONResponse({"id": uuid, "caption": answer, "url": f"https://7f0e-4-78-254-114.ngrok-free.app/render/{uuid}"})

if __name__ == "__main__":
    init_model()
    print("Starting server at http://localhost:8000")
    uvicorn.run(app, host="0.0.0.0", port=8000)
