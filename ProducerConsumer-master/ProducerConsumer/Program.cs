using System;
using System.Collections.Generic;
using System.Threading;

namespace ProducerConsumer
{
    class Program
    {
        // Буфер
        private readonly List<string> storage = new List<string>();
        private Semaphore Access;
        private Semaphore Full;
        private Semaphore Empty;

        static void Main(string[] args)
        {
            Program program = new Program();

            // Параметри: розмір буфера, загальна кількість продукції, кількість виробників, кількість споживачів
            program.Starter(storageSize: 50, totalItems: 500, producerCount: 80, consumerCount: 60);

            Console.WriteLine("All producers and consumers finished.");
            Console.ReadKey();
        }

        private void Starter(int storageSize, int totalItems, int producerCount, int consumerCount)
        {
            // Ініціалізація семафорів
            Access = new Semaphore(1, 1);                 // М'ютекс для доступу до буфера
            Full = new Semaphore(storageSize, storageSize); // Вільні місця
            Empty = new Semaphore(0, storageSize);        // Елементи для споживання

            List<Thread> threads = new List<Thread>();

            // Розподіляємо загальну кількість продукції випадково між виробниками
            List<int> producerDistribution = SplitRandomly(totalItems, producerCount);
            List<int> consumerDistribution = SplitRandomly(totalItems, consumerCount);

            for (int i = 0; i < producerCount; i++)
            {
                int itemsToProduce = producerDistribution[i];
                int producerId = i + 1;

                Thread producerThread = new Thread(() => Producer(itemsToProduce, producerId));
                threads.Add(producerThread);
                producerThread.Start();
            }

            for (int i = 0; i < consumerCount; i++)
            {
                int itemsToConsume = consumerDistribution[i];
                int consumerId = i + 1;

                Thread consumerThread = new Thread(() => Consumer(itemsToConsume, consumerId));
                threads.Add(consumerThread);
                consumerThread.Start();
            }

            // Очікуємо завершення всіх потоків
            foreach (var thread in threads)
            {
                thread.Join();
            }
        }

        private void Producer(int itemCount, int producerId)
        {
            for (int i = 0; i < itemCount; i++)
            {
                Full.WaitOne();       // Чекаємо вільне місце
                Access.WaitOne();     // Доступ до буфера

                string item = Guid.NewGuid().ToString();
                storage.Add(item);
                Console.WriteLine($"[Producer {producerId}] Added item {item}");

                Access.Release();     // Відпускаємо буфер
                Empty.Release();      // Сигнал для споживачів
            }
        }

        private void Consumer(int itemCount, int consumerId)
        {
            for (int i = 0; i < itemCount; i++)
            {
                Empty.WaitOne();      // Чекаємо, поки з’явиться щось у буфері
                Access.WaitOne();     // Доступ до буфера

                string item = storage[0];
                storage.RemoveAt(0);
                Console.WriteLine($"[Consumer {consumerId}] Took item {item}");

                Access.Release();     // Відпускаємо буфер
                Full.Release();       // Сигнал для виробників

                Thread.Sleep(300);   // Імітація обробки
            }
        }

        /// <summary>
        /// Генерує список випадкових чисел, які в сумі дорівнюють total, розміром parts
        /// </summary>
        private List<int> SplitRandomly(int total, int parts)
        {
            Random rnd = new Random();
            List<int> result = new List<int>();

            int sum = 0;
            for (int i = 0; i < parts - 1; i++)
            {
                int remaining = total - sum - (parts - i - 1); // мінімум 1 для кожного залишку
                int value = rnd.Next(1, remaining + 1);
                result.Add(value);
                sum += value;
            }

            result.Add(total - sum); // останній елемент щоб дорівнювати total

            // Перемішуємо, щоб уникнути шаблонного порядку
            for (int i = result.Count - 1; i > 0; i--)
            {
                int j = rnd.Next(i + 1);
                (result[i], result[j]) = (result[j], result[i]);
            }

            return result;
        }
    }
}