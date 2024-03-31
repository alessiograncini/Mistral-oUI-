import { Elysia, t, NotFoundError } from "elysia";

import { $ } from "bun";
import { z } from "zod";
import { zpp } from "@cryop/zpp";

import MistralClient from "@mistralai/mistralai";

import Anthropic from "@anthropic-ai/sdk";

import dedent from "dedent";

const interfaces: string[] = [];

const client = new MistralClient("key");

const anthropic = new Anthropic({
  apiKey:
    "key",
});

type Box = {
  name: string;
  x: number;
  y: number;
  width: number;
  height: number;
};

type DoneState = {
  type: "done";
  caption: string;
  url: string;
  box?: Box;
  htmlMarkup: string;
};

type ProcessingGrokState = {
  startTime: number;
  type: "processing_grok";
  image: File;
  caption: string;
};

type PreProcessingAnthropicState = {
  startTime: number;
  caption: string;
  type: "pre_processing_anthropic";
  image: File;
};

type ProcessingAnthropicState = {
  startTime: number;
  type: "processing_anthropic";
  image: File;
  labelImg: File;
  caption: string;
  objects: Box[];
};

type ProcessingMixtralState = {
  startTime: number;
  type: "processing_mixtral";
  caption: string;
  description: string;
  box?: Box;
};

type State =
  | {
      type: "idle" | "canceled";
    }
  | ProcessingGrokState
  | PreProcessingAnthropicState
  | ProcessingAnthropicState
  | ProcessingMixtralState
  | DoneState;

const processingEvents: Record<string, State> = {};
const objectData: Record<string, { objects: Box[]; image: File }> = {};

let lastId = "";

new Elysia()
  .onError(({ code, error }) => {
    console.warn("Error", { code, error });
  })
  .get("/health", () => "Hello Elysia")
  .post(
    "/newTick",
    ({ body }) => {
      const id = body.id;

      const state: ProcessingGrokState = {
        type: "processing_grok",
        image: body.image,
        startTime: performance.now(),
        caption: body.caption,
      };
      processingEvents[id] = state;

      handleNewTick(id, state);
    },
    {
      body: t.Object({ id: t.String(), image: t.File(), caption: t.String() }),
    }
  )
  .post("/free", ({ body }) => {
    lastId = "";
  })
  .get("/getResponse/:id", ({ params, set }) => {
    const id = params.id;

    const state = processingEvents[id];

    if (!state) {
      throw new NotFoundError();
    }

    if (state.type === "idle") {
      throw new NotFoundError();
    }

    if (state.type === "done" || state.type === "canceled") {
      return state;
    }

    return { type: "processing" };
  })
  .post(
    "/objectDetect",

    ({ body }) => {
      const { id, detected_objects } = body;
      const state = processingEvents[id];

      console.log("called objectDetect", { id, detected_objects });

      const detectedObjectsSchema = zpp(
        z.object({
          detected_objects: z.array(
            z.object({
              name: z.string(),
              xywh: z.tuple([z.number(), z.number(), z.number(), z.number()]),
            })
          ),
        })
      );

      const validatedDetectedObjects =
        detectedObjectsSchema.jsonParse(detected_objects);

      const objects = validatedDetectedObjects.detected_objects.map((obj) => {
        const [x, y, width, height] = obj.xywh;
        return {
          name: obj.name,
          x: x,
          y: y,
          width: width,
          height: height,
        } satisfies Box;
      });

      objectData[id] = {
        objects,
        image: body.image,
      };

      if (state.type === "pre_processing_anthropic") {
        console.log("pre_processing_anthropic");
        const newState: ProcessingAnthropicState = {
          startTime: state.startTime,
          type: "processing_anthropic",
          image: state.image,
          labelImg: body.image,
          caption: state.caption,
          objects: objects,
        };

        processHaiku(id, newState);
      }

      return { id };
    },
    {
      body: t.Object({
        id: t.String(),
        image: t.File(),
        detected_objects: t.String(),
      }),
    }
  )
  .get("/render/default", async ({ params, set }) => {
    const baseHTML = Bun.file("static/base.html");
    const loaderHTML = Bun.file("static/loader.html");

    const state = processingEvents[lastId];

    if (!state) {
      set.headers["content-type"] = "text/html";
      return await baseHTML.text();
    }

    if (state.type === "done") {
      set.headers["content-type"] = "text/html";

      return state.htmlMarkup;
    }

    set.headers["content-type"] = "text/html";
    return await loaderHTML.text();
  })
  .get("/render/:id", ({ params, set }) => {
    const id = params.id;

    const state = processingEvents[id];

    if (!state) {
      throw new NotFoundError();
    }

    if (state.type === "done") {
      set.headers["content-type"] = "text/html";

      return state.htmlMarkup;
    }

    return state;
  })
  .listen(3000);

// GROQ code
const handleNewTick = async (
  id: string,
  state: ProcessingGrokState,
  depth = 0
) => {
  try {
    const taskContext = "Not doing anything task currently";

    const perf = performance.now();

    const response = await client.chat({
      model: "mistral-small-latest",
      messages: [
        {
          role: "system",
          content: dedent`You are an agent that determines if the current frame (from the user's eye level) contains a useful interface to help the user. Decide if this 2-second tick matters. Task context provides information about the user's activity in the last 5 minutes. If the task context is "none," no interface has been shown to the user recently.
          Examples of relevant ticks and their potential interfaces:
          
          - Sitting down and seeing a TV -> Media controls or streaming service logos
          - Riding a bike outside -> Map or navigation interface
          - Being in a kitchen and seeing a stove -> Cooking interface, recipe book, or shopping list
          0 Looking at food -> Shopping list or calorie tracker
          
          Return in the following format ONLY:
          <reasoning> Your brief step-by-step reasoning if the frame is relevant </reasoning>
          <tick_matters>
          0 = irrelevant; 1 = relevant. ONLY RETURN 1 OR 0, NO COMMENTARY;
          </tick_matters>
        `,
        },
        {
          role: "user",
          content: dedent`Caption: ${state.caption} Task Context: ${taskContext}`,
        },
      ],
    });

    const messageContent = response.choices[0]?.message?.content;

    if (!messageContent) throw new Error("Invalid message content");

    const tickMattersMatch = messageContent.match(
      /<tick_matters>\s*(0|1)\s*<\/tick_matters>/
    );
    const reasoningMatch = messageContent.match(
      /<reasoning>(.*?)<\/reasoning>/s
    );

    console.log({ tickMattersMatch, reasoningMatch, messageContent });

    if (!tickMattersMatch || !reasoningMatch) {
      throw new Error("Tick matters data or reasoning is missing or invalid");
    }

    const tickMatters = Number(tickMattersMatch[1]);
    const reasoning = reasoningMatch[1].trim();

    console.log(
      "does tick matters",
      (performance.now() - perf) / 1000 + " seconds",
      {
        id,
        tick_matters: tickMatters,
        reasoning: reasoning,
      }
    );

    if (tickMatters === 1) {
      // TODO: add logic to determine if the tick matters

      if (objectData[id]) {
        console.log("objectData exits", id);
        const newState: ProcessingAnthropicState = {
          startTime: state.startTime,
          type: "processing_anthropic",
          image: state.image,
          caption: state.caption,
          objects: objectData[id].objects,
          labelImg: objectData[id].image,
        };

        processingEvents[id] = newState;

        return processHaiku(id, newState);
      }

      const newState: PreProcessingAnthropicState = {
        startTime: state.startTime,
        type: "pre_processing_anthropic",
        caption: state.caption,
        image: state.image,
      };

      processingEvents[id] = newState;
    }

    processingEvents[id] = {
      type: "canceled",
    };
  } catch (e) {
    if (depth > 2) {
      console.log("Exceed retries cancelling", id);
      processingEvents[id] = {
        type: "canceled",
      };
      return;
    }

    console.error("Error processing tick", e);
    console.log("Retrying");
    handleNewTick(id, state, depth + 1);
  }
};

const processHaiku = async (id: string, state: ProcessingAnthropicState) => {
  const image1_media_type = "image/jpeg";
  const image1_data = Buffer.from(await state.image.arrayBuffer()).toString(
    "base64"
  );

  const image2_media_type = "image/jpeg";
  const image2_data = Buffer.from(await state.labelImg.arrayBuffer()).toString(
    "base64"
  );

  const perf = performance.now();

  const response = await anthropic.messages.create({
    model: "claude-3-haiku-20240307",
    max_tokens: 1024,
    system: dedent`You are an assistant that helps determine how to assist the user based on the current frame from their AR glasses. Your goal is to design a useful, minimal interface projected as a translucent 2D plane onto the user's field of view.

    Describe the interface in intricate detail, as if it were a product spec. The interface should be as simple as possible while still being useful. If there is no useful interface, return </no_interface>.
    
    Examples of situations where you might help the user:
    
    Looking at food -> Shopping list, calorie tracker, or calorie count
    Sitting on the couch and seeing a TV -> Media controls or streaming service logos near the TV
    Riding a bike outside -> Map of the city and navigation interface
    In the kitchen, seeing a stove -> Cooking interface, recipe book, shopping list, or grocery list
    At the gym, lifting weights -> Workout interface
    Doing homework -> Homework assistance
    In a park -> Encourage them to touch grass and remove their headset
    Return in the following format:
    
    <reasoning> Think step by step about how you can help the user. What is the most useful interface you can think of? What are the key features of this interface? Think through the data in the interface, it is static and you should provide it. </reasoning> 
    <interface> Detailed description of the interface </interface>
    <focused_object> The index of the object you are focusing on, the interface will be position relative to this object. </focused_object>
    `,
    messages: [
      {
        role: "user",
        content: [
          {
            type: "text",
            text: dedent`Below are two images, one labels with object detection, one raw.
            
            Here is the list of the names of the object detected
            ${state.objects.map((obj, idx) => `${idx}. ${obj.name}`).join("\n")}
            `,
          },
          {
            type: "image",
            source: {
              type: "base64",
              media_type: image1_media_type,
              data: image1_data,
            },
          },
          {
            type: "image",
            source: {
              type: "base64",
              media_type: image2_media_type,
              data: image2_data,
            },
          },
        ],
      },
    ],
  });

  const data = response.content.pop();

  if (!data?.text) {
    console.log({ data });
    throw new Error("Invalid data format");
  }

  if (data.text.includes("</no_interface>")) {
    processingEvents[id] = {
      type: "canceled",
    };
    return;
  }

  const interfaceMatch = data.text.match(/<interface>(.*?)<\/interface>/s);
  const focusedObjectMatch = data.text.match(
    /<focused_object>(.*?)<\/focused_object>/s
  );

  if (!interfaceMatch) {
    throw new Error("Invalid interface format");
  }
  const interfaceText = interfaceMatch[1].trim();

  let box: Box | undefined;
  if (focusedObjectMatch) {
    const focusedObject = focusedObjectMatch[1].trim();
    box = state.objects[Number(focusedObject) - 1];
  }

  console.log(
    `Generated interface text for ${id}`,
    (performance.now() - perf) / 1000,
    "seconds",
    { interfaceText }
  );

  if (!interfaceText) {
    processingEvents[id] = {
      type: "canceled",
    };
    return;
  }

  interfaces.push(interfaceText);

  const newState: ProcessingMixtralState = {
    startTime: state.startTime,
    caption: state.caption,
    type: "processing_mixtral",
    description: interfaceText,
    box,
  };

  processingEvents[id] = newState;

  handleSceneGeneration(id, newState);
};

const handleSceneGeneration = async (
  id: string,
  state: ProcessingMixtralState
) => {
  // TODO: add logic to generate scene

  const perf = performance.now();

  const response = await client.chat({
    model: "mistral-large-latest",
    messages: [
      {
        role: "system",
        content: dedent`You are an agent that generates HTML with Tailwind CSS based on a project spec. 
        The generated UI will be translucent and rendered in AR, so most high-level components (like cards) should have black backgrounds with borders and shadows instead of different background colors.
        
        Guidelines:
        - Everything should be in dark mode, the body's background color will be black
        - Do not use external assets or inline images; ignore them or add your own SVGs
        - Stick to flexbox as much as possible
        - Return ONLY the HTML and Tailwind CSS that goes inside the <body> tags, without closing the </body> tag; DO NOT include <script> tags, or providing any commentary before the HTML fragment`,
      },
      {
        role: "user",
        content: `Project spec: ${state.description}`,
      },
    ],
  });

  const generatedHtml = response.choices[0]?.message?.content;
  const bodyContentMatch = generatedHtml.match(/<body>(.*?)<\/body>/s);

  console.log(
    `Generated html for ${id}`,
    (performance.now() - perf) / 1000,
    "seconds"
  );

  if (bodyContentMatch) {
    const bodyContent = bodyContentMatch[1];
    const html = dedent`<!DOCTYPE html>
    <html lang="en">
    
    <head>
        <meta charset="UTF-8">
        <meta name="viewport" content="width=device-width, initial-scale=1.0">
        <title>Document</title>
        <script src="https://cdn.tailwindcss.com"></script>
    
    </head>
    
    <body class="bg-black">
    ${bodyContent}
    </body>
    
    </html>`;

    Bun.write(`generated/index.${id}.html`, html);

    processingEvents[id] = {
      caption: state.caption,
      type: "done",
      url: `http://localhost:3000/render/${id}`,
      box: state.box,
      htmlMarkup: html,
    };
  } else {
    const html = dedent`<!DOCTYPE html>
  <html lang="en">
  
  <head>
      <meta charset="UTF-8">
      <meta name="viewport" content="width=device-width, initial-scale=1.0">
      <title>Document</title>
      <script src="https://cdn.tailwindcss.com"></script>
  
  </head>
  
  <body class="bg-black">
  ${generatedHtml}
  </body>
  
  </html>`;

    Bun.write(`generated/index.${id}.html`, html);

    processingEvents[id] = {
      type: "done",
      box: state.box,
      url: `http://localhost:3000/render/${id}`,
      caption: state.caption,
      htmlMarkup: html,
    };
  }

  console.log(
    `Total time taken for ${id}`,
    (performance.now() - state.startTime) / 1000,
    "seconds"
  );

  if (!lastId) {
    lastId = id;
  }

  await $`open http://localhost:3000/render/${id}`;
};

console.log("Listening on port 3000");