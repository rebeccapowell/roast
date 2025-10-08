import { memo, useMemo } from "react";

type YouTubeEmbedProps = {
  videoId: string;
  title: string;
  className?: string;
};

export const YouTubeEmbed = memo(function YouTubeEmbed({ videoId, title, className }: YouTubeEmbedProps) {
  const src = useMemo(
    () => `https://www.youtube.com/embed/${videoId}?rel=0&modestbranding=1`,
    [videoId],
  );

  return (
    <iframe
      className={className}
      src={src}
      title={title}
      allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share"
      allowFullScreen
    />
  );
});
