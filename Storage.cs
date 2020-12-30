using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace lab16
{
	// Склад
	class Storage
	{
		public Storage()
		{
			Products = new BlockingCollection<Product>();
		}

		public BlockingCollection<Product> Products { get; private set; }

		// Добавление товара
		public void Add(Product product)
		{
			Products.Add(product);
			Console.WriteLine($"+ {product.Name}\n");
			PrintProducts();
		}

		// Попытка получения товара
		public Product Take()
		{
			var product = Products.Take();
			if (product != null)
			{
				Console.WriteLine($"- {product.Name}\n");
				PrintProducts();
			}

			return product;
		}

		// Вывод товаров
		public void PrintProducts()
		{
			Console.Clear();
			foreach (var item in Products.OrderBy(item => item.Id))
				Console.WriteLine($"{item.Name,-20} {item.Id}");
		}
	}

	// Продукт
	class Product
	{
		static readonly Random Rdnm = new Random();

		public string Name { get; }
		public int Id { get; }
		
		public Product(string name)
		{
			Name = name;
			Id = Rdnm.Next(100000, 1000000);
		}

		public override string ToString() => $"{Name} {Id}";
	}

	// Поставщик
	class Supplier
	{
		Task _task;

		// Поставляемый товар
		public string Product { get; set; }
		// Интервал поставки
		public int Interval { get; set; }
		// Склады
		public List<Storage> Storages { get; private set; }
		// Поставляет ли сейчас
		public bool NowSupplies { get; private set; }
		
		public Supplier(string product, int interval, params Storage[] storages)
		{
			Product = product;
			Interval = interval;
			Storages = new List<Storage>(storages);
		}

		// Начать поставки
		public void StartDeliveries()
		{
			NowSupplies = true;
			_task = new Task(() =>
			{
				// Поставлять пока состояние не изменится извне
				while (NowSupplies)
				{
					// Добавление товара на склады
					Storages.ForEach(storage => storage.Add(new Product(Product)));
					Thread.Sleep(Interval);
				}
			});
			_task.Start();
		}

		// "Команда" остановки поставок
		public void FinishDeliveries() => NowSupplies = false;
	}

	// Покупатель
	class Consumer
	{
		Task _task;

		public Consumer(int interval, Storage storage)
		{
			Interval = interval;
			Storage = storage;
		}

		public int Interval { get; set; }
		public Storage Storage { get; private set; }
		public bool NowBuying { get; private set; }

		public void StartBuying()
		{
			NowBuying = true;
			_task = new Task(() =>
			{
				// Попытаться купить что-либо с интервалом в 5 сек
				Thread.Sleep(5000);
				while (NowBuying)
				{
					Storage.Take();
					Thread.Sleep(Interval);
				}
			});
			_task.Start();
		}

		public void FinishBuying() => NowBuying = false;
	}
}