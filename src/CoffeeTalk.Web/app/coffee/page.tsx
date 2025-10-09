import Link from 'next/link';
import { getCoffeeBars } from './_data';

export const revalidate = 60;

export default async function CoffeeListPage() {
  const bars = await getCoffeeBars();

  return (
    <main>
      <h1>Coffee Bars</h1>
      <ul>
        {bars.map((bar) => (
          <li key={bar.id}>
            <Link href={`/coffee/${bar.id}`}>{bar.name}</Link>
          </li>
        ))}
      </ul>
    </main>
  );
}
