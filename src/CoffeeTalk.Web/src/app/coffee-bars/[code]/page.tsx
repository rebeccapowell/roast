import { CoffeeBarClient } from "@/app/coffee-bars/[code]/CoffeeBarClient";

type CoffeeBarPageProps = {
  params: Promise<{ code: string }>;
};

export default async function CoffeeBarPage({ params }: CoffeeBarPageProps) {
  const { code } = await params;
  return <CoffeeBarClient code={code} />;
}
