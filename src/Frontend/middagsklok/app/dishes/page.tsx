import Sidebar from "../components/Sidebar";

type Dish = {
  id: string;
  name: string;
  cuisine: string;
  prepMinutes: number;
  cookMinutes: number;
  serves: number;
  ingredients: string[];
};

const dishes: Dish[] = [
  {
    id: "spaghetti-carbonara",
    name: "Spaghetti Carbonara",
    cuisine: "Italian",
    prepMinutes: 10,
    cookMinutes: 20,
    serves: 4,
    ingredients: ["400 g Spaghetti", "4 pcs Eggs", "200 g Bacon", "50 g Parmesan"],
  },
  {
    id: "thai-green-curry",
    name: "Thai Green Curry",
    cuisine: "Asian",
    prepMinutes: 15,
    cookMinutes: 25,
    serves: 4,
    ingredients: [
      "500 g Chicken breast",
      "400 ml Coconut milk",
      "3 tbsp Green curry paste",
      "1 bunch Basil",
    ],
  },
  {
    id: "greek-salad",
    name: "Greek Salad",
    cuisine: "Mediterranean",
    prepMinutes: 15,
    cookMinutes: 0,
    serves: 4,
    ingredients: ["4 pcs Tomatoes", "1 pcs Cucumber", "200 g Feta cheese", "1 tsp Oregano"],
  },
  {
    id: "beef-tacos",
    name: "Beef Tacos",
    cuisine: "Mexican",
    prepMinutes: 15,
    cookMinutes: 20,
    serves: 4,
    ingredients: [
      "500 g Ground beef",
      "12 pcs Taco shells",
      "200 g Lettuce",
      "100 g Cheddar",
    ],
  },
  {
    id: "chicken-tikka-masala",
    name: "Chicken Tikka Masala",
    cuisine: "Indian",
    prepMinutes: 30,
    cookMinutes: 40,
    serves: 6,
    ingredients: ["800 g Chicken breast", "2 pcs Eggs", "150 ml Cream"],
  },
  {
    id: "caesar-salad",
    name: "Caesar Salad",
    cuisine: "American",
    prepMinutes: 10,
    cookMinutes: 0,
    serves: 2,
    ingredients: ["1 head Lettuce", "50 g Parmesan cheese", "150 g Croutons"],
  },
];

const formatMinutes = (value: number) => `${value}m`;

export default function DishesPage() {
  return (
    <div className="min-h-screen w-full p-6 sm:p-8">
      <div className="flex flex-wrap items-start gap-6">
        <Sidebar />
        <main className="flex-1 min-w-[280px] space-y-6">
          <header className="flex flex-wrap items-start justify-between gap-4">
            <div className="flex items-center gap-3">
              <span className="grid h-12 w-12 place-items-center rounded-2xl bg-[#f0f4ee] text-[#2f6b4f]">
                <DishIcon className="h-5 w-5" />
              </span>
              <div>
                <h1 className="text-2xl font-semibold text-[#1f2a22]">
                  Dishes
                </h1>
                <p className="text-sm text-[#6c7a70]">
                  Manage your recipe collection
                </p>
              </div>
            </div>
            <div className="flex flex-wrap items-center gap-3">
              <button
                type="button"
                className="inline-flex items-center gap-2 rounded-full border border-[#d6e0d2] bg-white px-4 py-2 text-sm font-semibold text-[#3b4c42] transition hover:bg-[#f3f6ef]"
              >
                <ImportIcon className="h-4 w-4" />
                Import dishes
              </button>
              <button
                type="button"
                className="inline-flex items-center gap-2 rounded-full bg-[#2f6b4f] px-4 py-2 text-sm font-semibold text-white shadow-[0_12px_24px_-18px_rgba(32,78,54,0.9)] transition hover:bg-[#2a5c46]"
              >
                <PlusIcon className="h-4 w-4" />
                Add dish
              </button>
            </div>
          </header>

          <div className="flex items-center gap-3 rounded-2xl border border-[#e1e8dc] bg-white/80 px-4 py-3 shadow-[0_10px_30px_-26px_rgba(30,60,40,0.4)]">
            <SearchIcon className="h-4 w-4 text-[#7a887f]" />
            <input
              type="text"
              aria-label="Search dishes"
              placeholder="Search dishes by name or category..."
              className="w-full bg-transparent text-sm text-[#2e3b33] placeholder:text-[#9aa69f] focus:outline-none"
            />
          </div>

          <section className="grid gap-5 lg:grid-cols-2 xl:grid-cols-3">
            {dishes.map((dish) => {
              const previewIngredients = dish.ingredients.slice(0, 3);
              const remainingCount = dish.ingredients.length - previewIngredients.length;

              return (
                <article
                  key={dish.id}
                  className="rounded-2xl border border-[#e3eadf] bg-white/80 p-5 shadow-[0_16px_40px_-30px_rgba(35,60,42,0.35)]"
                >
                  <div className="flex items-start justify-between gap-4">
                    <div>
                      <h2 className="text-base font-semibold text-[#1f2a22]">
                        {dish.name}
                      </h2>
                      <span className="mt-2 inline-flex rounded-full bg-[#edf1ea] px-3 py-1 text-xs font-semibold text-[#4f5f55]">
                        {dish.cuisine}
                      </span>
                    </div>
                    <div className="flex items-center gap-2">
                      <button
                        type="button"
                        aria-label={`Edit ${dish.name}`}
                        className="grid h-9 w-9 place-items-center rounded-full border border-[#e3eadf] text-[#6e7c72] transition hover:bg-[#f4f7f1]"
                      >
                        <EditIcon className="h-4 w-4" />
                      </button>
                      <button
                        type="button"
                        aria-label={`Delete ${dish.name}`}
                        className="grid h-9 w-9 place-items-center rounded-full border border-[#f0dada] text-[#d76b6b] transition hover:bg-[#fbeeee]"
                      >
                        <TrashIcon className="h-4 w-4" />
                      </button>
                    </div>
                  </div>

                  <div className="mt-4 flex flex-wrap items-center gap-3 text-xs font-semibold text-[#6c7a70]">
                    <span className="flex items-center gap-2">
                      <TimerIcon className="h-4 w-4 text-[#2f6b4f]" />
                      Prep: {formatMinutes(dish.prepMinutes)}
                    </span>
                    <span className="flex items-center gap-2">
                      <PanSmallIcon className="h-4 w-4 text-[#2f6b4f]" />
                      Cook: {formatMinutes(dish.cookMinutes)}
                    </span>
                    <span className="flex items-center gap-2">
                      <PeopleIcon className="h-4 w-4 text-[#2f6b4f]" />
                      Serves: {dish.serves}
                    </span>
                  </div>

                  <div className="mt-4">
                    <div className="text-xs font-semibold uppercase tracking-[0.18em] text-[#7a887f]">
                      Ingredients
                    </div>
                    <ul className="mt-3 space-y-2 text-sm text-[#3d4c43]">
                      {previewIngredients.map((ingredient) => (
                        <li key={ingredient} className="flex items-center gap-2">
                          <span className="h-2 w-2 rounded-full bg-[#9bb09f]" />
                          {ingredient}
                        </li>
                      ))}
                      {remainingCount > 0 ? (
                        <li className="text-xs font-semibold text-[#6d7b72]">
                          + {remainingCount} more...
                        </li>
                      ) : null}
                    </ul>
                  </div>
                </article>
              );
            })}
          </section>
        </main>
      </div>
    </div>
  );
}

function DishIcon({ className }: { className?: string }) {
  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
    >
      <path d="M4.5 6h15" />
      <path d="M6.5 10h11" />
      <path d="M8.5 14h7" />
      <path d="M10 18h4" />
    </svg>
  );
}

function SearchIcon({ className }: { className?: string }) {
  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
    >
      <circle cx="11" cy="11" r="6.5" />
      <path d="M16 16l4 4" />
    </svg>
  );
}

function PlusIcon({ className }: { className?: string }) {
  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      strokeLinecap="round"
      className={className}
    >
      <path d="M12 5v14M5 12h14" />
    </svg>
  );
}

function ImportIcon({ className }: { className?: string }) {
  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
    >
      <path d="M12 3v12" />
      <path d="m7 8 5-5 5 5" />
      <path d="M5 15v4a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2v-4" />
    </svg>
  );
}

function EditIcon({ className }: { className?: string }) {
  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
    >
      <path d="M5 19h4l10-10-4-4L5 15v4Z" />
      <path d="M13 5l4 4" />
    </svg>
  );
}

function TrashIcon({ className }: { className?: string }) {
  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
    >
      <path d="M4 7h16" />
      <path d="M9 7V5h6v2" />
      <path d="m7 7 1 12h8l1-12" />
      <path d="M10 11v6M14 11v6" />
    </svg>
  );
}

function TimerIcon({ className }: { className?: string }) {
  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
    >
      <circle cx="12" cy="13" r="7" />
      <path d="M12 13V9" />
      <path d="M9 3h6" />
    </svg>
  );
}

function PanSmallIcon({ className }: { className?: string }) {
  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
    >
      <path d="M6 12h9a3.5 3.5 0 0 1 0 7H7.5A1.5 1.5 0 0 1 6 17.5V12Z" />
      <path d="M15 12h4a1.5 1.5 0 0 0 0-3h-4" />
    </svg>
  );
}

function PeopleIcon({ className }: { className?: string }) {
  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
    >
      <path d="M8.5 11.5a3.5 3.5 0 1 0 0-7 3.5 3.5 0 0 0 0 7Z" />
      <path d="M15.5 12.5a3 3 0 1 0 0-6 3 3 0 0 0 0 6Z" />
      <path d="M3.5 19.5a5.5 5.5 0 0 1 10 0" />
      <path d="M14 19.5a4.5 4.5 0 0 1 6 0" />
    </svg>
  );
}
