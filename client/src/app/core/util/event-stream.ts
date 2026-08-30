const STREAM_PREFIX = 'data: ';

const parseChunk = <TEvent>(chunk: string): TEvent | null => {
  const line = chunk.trim();

  if (!line.startsWith(STREAM_PREFIX)) {
    return null;
  }

  try {
    return JSON.parse(line.slice(STREAM_PREFIX.length)) as TEvent;
  } catch {
    return null;
  }
};

export const readEventStream = async <TEvent>(
  body: ReadableStream<Uint8Array>,
  onEvent: (event: TEvent) => void
) => {
  const reader = body.getReader();
  const decoder = new TextDecoder();
  let buffer = '';

  for (;;) {
    const { done, value } = await reader.read();

    if (done) {
      return;
    }

    buffer += decoder.decode(value, { stream: true });

    const chunks = buffer.split('\n\n');
    buffer = chunks.pop() ?? '';

    for (const chunk of chunks) {
      const event = parseChunk<TEvent>(chunk);

      if (event) {
        onEvent(event);
      }
    }
  }
};
