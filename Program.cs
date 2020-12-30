using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable ReturnValueOfPureMethodIsNotUsed
// ReSharper disable AccessToModifiedClosure

namespace lab16
{
	class Program
	{
		static readonly List<Action> Tasks = new List<Action> {
			Task1,
			Task2,
			Task3_4,
			Task5,
			Task6,
			Task7,
			Task8
		};

		static void Main()
		{
			while (true)
			{
				Console.Write(
					"1 - длительная по времени задача" +
					"\n2 - с токеном отмены" +
					"\n3 - задача продолжения" +
					"\n4 - распаралеллить вычисление циклов" +
					"\n5 - распараллелить выполнение блока операторов" +
					"\n6 - склад с BlockingCollection" +
					"\n7 - async await" +
					"\n0 - выход" +
					"\nВыберите действие: "
					);
				if (!int.TryParse(Console.ReadLine(), out int choice) || choice < 0 || choice > Tasks.Count)
				{
					Console.WriteLine("Нет такого действия");
					Console.ReadKey();
					Console.Clear();
					continue;
				}
				if (choice == 0)
				{
					Console.WriteLine("Выход...");
					Environment.Exit(0);
				}
				Tasks[choice - 1]();
				Console.ReadKey();
				Console.Clear();
			}
		}

		static void Task1()
		{
			// Новая задача подсчёта простых чисел методом эратосфена
			var task = new Task(() => PrimeNumbers.Eratosfen(250000));
			task.Start();
			Console.WriteLine("ID текущей задачи: " + task.Id);

			int counter = 1;
			int interval = 1000;
			// Сообщения состояния задачи
			do
			{
				Thread.Sleep(interval);
				Console.WriteLine($"Прошло {counter++ * interval / 1000} сек, задача ещё не выполнена");
			} while (task.Status == TaskStatus.Running);
			Console.WriteLine("Выполнение задачи завершено");

			// Подсчёт производительности
			int run = 5;
			int max = 100000;

			long sumE = 0, sumS = 0;

			Console.WriteLine($"Подсчёт производительности на нескольких прогонах ({run}), с максимальным числом {max} (мс)");
			// Счётчик для замера времени
			var sw = new Stopwatch();
			Console.WriteLine("Эратосфен    Не Эратосфен");
			for (int i = 0; i < run; i++)
			{
				// Замер методоа эратосфена
				sw.Start();
				task = new Task(() => PrimeNumbers.Eratosfen(max));
				task.Start();
				task.Wait();
				sw.Stop();
				long elE = sw.ElapsedMilliseconds;
				sumE += elE;

				sw.Reset();

				// Замер простого метода
				sw.Start();
				task = new Task(() => PrimeNumbers.Simple(max));
				task.Start();
				task.Wait();
				sw.Stop();
				long elS = sw.ElapsedMilliseconds;
				sumS += elS;

				Console.WriteLine($"{elE,6}{elS,15}");
			}
			Console.WriteLine($"Среднее время:\nЭратосфен: {sumE / run}мс\nПоследовательный алгоритм: {sumS / run}мс");
		}

		static void Task2()
		{
			// То же что и в 1ом задании, но с токеном отмены
			var eratosfen = new Action<object, CancellationToken>(PrimeNumbers.Eratosfen);

			var tokenSrc = new CancellationTokenSource();
			var token = tokenSrc.Token;

			var task = new Task(() => eratosfen(1000000, token));
			task.Start();
			Console.WriteLine("ID текущей задачи: " + task.Id);

			int counter = 1;
			int interval = 1000;
			do
			{
				Thread.Sleep(interval);
				Console.WriteLine($"Прошло {counter * interval / 1000} сек, задача ещё не выполнена");
				if (counter++ == 5)
				{
					// Изменение состояния токена после 5 сообщений
					tokenSrc.Cancel();
					Console.WriteLine("Отмена");
					break;
				}
			} while (task.Status == TaskStatus.Running);
			Console.WriteLine("Выполнение задачи завершено");
		}

		static void Task3_4()
		{
			// Функция для примера
			var func = new Func<int, int>(x => { Thread.Sleep(1000 * x); return x + 1; });

			Console.WriteLine("ContinueWith");
			var task1 = new Task<int>(() => func(1));
			task1.Start();
			var task2 = task1.ContinueWith(task => func(task.Result));
			var task3 = task2.ContinueWith(task => func(task.Result));
			task3.Wait();
			Console.WriteLine("Полученное число: " + task3.Result);

			Console.WriteLine("Awaiter");
			task1 = new Task<int>(() => func(1));
			task1.Start();
			var res = task1.GetAwaiter().GetResult();
			task2 = new Task<int>(() => func(res));
			task2.Start();
			res = task2.GetAwaiter().GetResult();
			task3 = new Task<int>(() => func(res));
			task3.Start();
			res = task3.GetAwaiter().GetResult();
			Console.WriteLine("Полученное число: " + res);
		}

		static void Task5()
		{
			int run = 5, amt = 10000000;

			var list = new List<double>();
			for (int i = 0; i < amt; i++)
				list.Add(i);

			long sumP = 0, sumS = 0;

			Console.WriteLine($"Подсчёт производительности на нескольких прогонах ({run}), при кол-во элементов {amt} (мс)");
			var sw = new Stopwatch();
			Console.WriteLine("Параллельно    Последовательно");
			for (int i = 0; i < run; i++)
			{
				// Подсчёт синуса каждого элемента параллельно
				sw.Start();
				Parallel.ForEach(list, DoSmth);
				sw.Stop();
				long elP = sw.ElapsedMilliseconds;
				sumP += elP;

				sw.Reset();

				// Подсчёт синуса каждого элемента последовательно
				sw.Start();
				list.ForEach(DoSmth);
				sw.Stop();
				long elS = sw.ElapsedMilliseconds;
				sumS += elS;

				Console.WriteLine($"{elP,6}{elS,17}");
			}
			Console.WriteLine($"Среднее время:\nПараллельно: {sumP / run}мс\nПоследовательно: {sumS / run}мс");

			// Подсчёт синуса
			static void DoSmth(double x) => Math.Sin(x);
		}

		static void Task6()
		{
			// Параллельное выполнение функции DoSmth и анонимной
			Parallel.Invoke(DoSmth, () => {
				for (int i = 0; i < 5; i++)
				{
					Console.WriteLine("Что-то происходит в первой задаче");
					Thread.Sleep(1000);
				}
			});

			static void DoSmth()
			{
				for (int i = 0; i < 10; i++)
				{
					Console.WriteLine("Что-то происходит во второй задаче");
					Thread.Sleep(500);
				}
			}
		}

		static void Task7()
		{
			var storage = new Storage();
			var suppliers = new List<Supplier>() {
				new Supplier("Телевизор", 2000, storage),
				new Supplier("Компьютер", 3000, storage),
				new Supplier("Холодильник", 2500, storage),
				new Supplier("Микроволновка", 4000, storage),
				new Supplier("Пылесос", 5000, storage)
			};
			var consumers = new List<Consumer>();
			var rndm = new Random();
			for (int i = 0; i < 10; i++)
				consumers.Add(new Consumer(rndm.Next(5000, 10000), storage));

			// Начало поставок / покупок
			suppliers.ForEach(item => item.StartDeliveries());
			consumers.ForEach(item => item.StartBuying());

			Console.ReadKey();
			// Завершение поставок / покупок
			suppliers.ForEach(item => item.FinishDeliveries());
			consumers.ForEach(item => item.FinishBuying());
		}

		static void Task8()
		{
			Console.WriteLine("Запрос времени...");
			try
			{
				Console.WriteLine("Время в Минске: " + GetTimeAsync().Result);
			}
			catch (Exception e)
			{
				Console.WriteLine($"Не удалось запросить время\n{e.Message}");
			}
		}

		static async Task<string> GetTimeAsync()
		{
			string res = await () => {
				Thread.Sleep(2000);
			}

			static string GetTime() =>
DateTime.Now.ToString("G");
		}
	}
}