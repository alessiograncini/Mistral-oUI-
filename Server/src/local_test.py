import threading
import io
import argparse
import json
import torch
import re
import time
import gradio as gr
from threading import Thread
from transformers import TextIteratorStreamer, AutoTokenizer, AutoModelForCausalLM
import requests


# LATEST_REVISION = "2024-03-06"
LATEST_REVISION = "2024-03-13"


def detect_device():
    """
    Detects the appropriate device to run on, and return the device and dtype.
    """
    if torch.cuda.is_available():
        return torch.device("cuda"), torch.float16
    elif torch.backends.mps.is_available():
        return torch.device("mps"), torch.float16
    else:
        return torch.device("cpu"), torch.float32


moondream = None
model_id = "vikhyatk/moondream2"


def init_model():
    global moondream
    parser = argparse.ArgumentParser()
    parser.add_argument("--cpu", action="store_true")
    args = parser.parse_args()

    if args.cpu:
        device = torch.device("cpu")
        dtype = torch.float32
    else:
        device, dtype = detect_device()
        if device != torch.device("cpu"):
            print("Using device:", device)
            print("If you run into issues, pass the `--cpu` flag to this script.")
            print()

    moondream = AutoModelForCausalLM.from_pretrained(
        model_id, trust_remote_code=True, revision=LATEST_REVISION
    ).to(device=device, dtype=dtype)
    moondream.eval()


def send_answer(caption, latest_image):
    def task():
        print("Sending caption:", caption)

        bytes_buffer = io.BytesIO()
        latest_image.save(bytes_buffer, format='JPEG')
        bytes_buffer.seek(0)

        response = requests.post(
            "http://localhost:3000/newTick",
            files={"image": ('image.jpeg', bytes_buffer)},
            data={"caption": caption}
        )
        print("Response ID:", response.json().get("id"))

    threading.Thread(target=task).start()
    # time.sleep(5)


def answer_question(img, prompt):
    global answer
    global moondream
    tokenizer = AutoTokenizer.from_pretrained(
        model_id, revision=LATEST_REVISION)
    image_embeds = moondream.encode_image(img)
    streamer = TextIteratorStreamer(tokenizer, skip_special_tokens=True)
    thread = Thread(
        target=moondream.answer_question,
        kwargs={
            "image_embeds": image_embeds,
            "question": prompt,
            "tokenizer": tokenizer,
            "streamer": streamer,
        },
    )
    thread.start()

    buffer = ""
    for new_text in streamer:
        clean_text = re.sub("<$|END$", "", new_text)
        buffer += clean_text
        yield buffer.strip("<END")


if __name__ == "__main__":
    with gr.Blocks() as demo:
        gr.Markdown("# 🗿 gigachad")

        gr.HTML(
            """
            <style type="text/css">
                .md_output p {
                    padding-top: 1rem;
                    font-size: 1.2rem !important;
                }
            </style>
            """
        )

        with gr.Row():
            prompt = gr.Textbox(
                label="Prompt",
                value="What do you see? Keep it brief and articulate, comment specifically on things that look in motion or have likely just changed, or on things the user is looking at. Assume the image is from the user's pov.",
                interactive=True,
            )
        with gr.Row():
            img = gr.Image(type="pil", label="Upload an Image", streaming=True)
            output = gr.Markdown(elem_classes=["md_output"])

        latest_img = None
        latest_prompt = prompt.value

        @img.change(inputs=[img])
        def img_change(img):
            global latest_img
            latest_img = img

        @prompt.change(inputs=[prompt])
        def prompt_change(prompt):
            global latest_prompt
            latest_prompt = prompt

        @demo.load(outputs=[output])
        def live_video():
            global latest_img
            while True:
                if latest_img is None:
                    print("waiting for new image")
                    time.sleep(0.1)
                else:
                    buffer = ""
                    for text in answer_question(latest_img, latest_prompt):
                        if len(text) > 0:
                            buffer = text
                            yield text
                    send_answer(buffer, latest_img)
                    latest_img = None

    init_model()
    demo.queue().launch(debug=True)
