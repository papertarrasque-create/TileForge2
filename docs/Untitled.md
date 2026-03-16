## Specs

- 9B dense parameters, 32 layers
- Hybrid architecture: Gated DeltaNet linear attention + full softmax attention (3:1 ratio)
- 262K native context (extendable to 1M with YaRN)
- Natively multimodal (text, image, video)
- Multi-token prediction (MTP) support
- 248K vocabulary, 201 languages
- Based on Qwen3.5-9B

## Recommended Settings

From the official Qwen authors:

**Thinking mode (default):**

- `temperature=0.6`, `top_p=0.95`, `top_k=20`, `min_p=0`

**Non-thinking mode:**

- `temperature=0.7`, `top_p=0.8`, `top_k=20`, `min_p=0`

**Important:**

- Maintain at least 128K context to preserve thinking capabilities
- For production/high-throughput: use vLLM, SGLang, or KTransformers

**Note:** This is a brand new architecture (released 2026-03-02). llama.cpp support landed very recently — make sure you're on a recent build. Works with llama.cpp, LM Studio, Jan, koboldcpp, etc.

Also check out the 4B variant and all releases at HauhauCS.