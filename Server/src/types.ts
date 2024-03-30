import { object, z } from "zod";

const unityPrimitiveTypes = z.enum([
  "Cube",
  "Sphere",
  "Capsule",
  "Cylinder",
  "Plane",
  "Quad",
]);

type UnityPrimitiveTypes = z.infer<typeof unityPrimitiveTypes>;

const threeDvector = z.array(z.number());

export type ThreeDvector = z.infer<typeof threeDvector>;

export const RGBASchema = z
  .string()
  .refine((val) =>
    val.match(/^rgba\(\d{1,3},\s\d{1,3},\s\d{1,3},\s*(0?\.\d+|1(\.0+)?)\)$/)
  );
export type RGBAType = z.infer<typeof RGBASchema>;

export type PrimitiveType = {
  type: UnityPrimitiveTypes;
  color: RGBAType;
  position: ThreeDvector;
  scale: ThreeDvector;
  children?: PrimitiveType[];
};

export const sceneObjectSchema: z.ZodType<PrimitiveType> = z.lazy(() =>
  z.object({
    type: unityPrimitiveTypes,
    color: RGBASchema.describe("rgba color"),
    position: threeDvector,
    scale: threeDvector,
    children: z.array(sceneObjectSchema),
  })
);
