# zpp

Tiny wrapper around `zod` to make it more ergonomic to use - mostly for me tbh

## Usage


1. JSON Parse - don't worry about if it a `string` or `object` - just parse it
```ts
export const schema = zpp(
  z.object({
    userId: z.string(),
    nextPageToken: z.string()
  })
)

const handler: NextApiHandler = async (req, res) => {
  const { userId, nextPageToken } = schema.jsonParse(req.body)
    // ...
}

export default handler
```

2. New
```ts
const schema = zpp(
  z.object({
    userId: z.string(),
    nextPageToken: z.string()
  })
)

// Give you types when creating new objects
const data = schema.new({
  userId: '123',
  nextPageToken: '456'
})
```

3. Stringify
```ts
const schema = zpp(
  z.object({
    userId: z.string(),
    nextPageToken: z.string()
  })
)

// Give you types when creating new objects
const stringified = schema.stringify({
  userId: '123',
  nextPageToken: '456'
})
```

4. toOpenAiFuncSchema
```ts
const schema = zpp(
  z.object({
    userId: z.string(),
    nextPageToken: z.string()
  })
)


const openAiSchema = schema.toOpenAiFuncSchema()
```

5. toPrompt
```ts
const schema = zpp(
  z.object({
    userId: z.string().describe("The user's id"),
    nextPageToken: z.string("The next page token")
  })
)

const prompt = schema.toPrompt()
/*
{
    userId: "The user's id", // The user's id
    nextPageToken: "The next page token" // The next page token
}
*/
```

## Acknowledgement/License

Huge shout out to zod, this is just a light wrapper around it to make it more ergonomic for me to use

MIT License