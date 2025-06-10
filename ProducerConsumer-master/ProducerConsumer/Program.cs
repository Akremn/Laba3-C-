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
            program.Starter(storageSize: 5, totalItems: 50, producerCount: 8, consumerCount: 6);

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
                Console.WriteLine($"[Producer {producerId}] Чекає вільне місце...");
                Full.WaitOne();     
                Console.WriteLine($"[Producer {producerId}] Місце знайдено.");

                Console.WriteLine($"[Producer {producerId}] Очікує доступ до буфера...");
                Access.WaitOne();   
                Console.WriteLine($"[Producer {producerId}] Отримав доступ до буфера.");

                string item = Guid.NewGuid().ToString();
                storage.Add(item);
                Console.WriteLine($"[Producer {producerId}] Додав елемент: {item}");

                Access.Release();    
                Console.WriteLine($"[Producer {producerId}] Звільнив доступ до буфера.");

                Empty.Release();    
                Console.WriteLine($"[Producer {producerId}] Повідомив, що з’явився новий елемент.");
            }
        }

        private void Consumer(int itemCount, int consumerId)
        {
            for (int i = 0; i < itemCount; i++)
            {
                Console.WriteLine($"[Consumer {consumerId}] Очікує, поки з’явиться елемент у буфері...");
                Empty.WaitOne(); 
                Console.WriteLine($"[Consumer {consumerId}] Знайдено елемент у буфері.");

                Console.WriteLine($"[Consumer {consumerId}] Очікує доступ до буфера...");
                Access.WaitOne(); 
                Console.WriteLine($"[Consumer {consumerId}] Отримано доступ до буфера.");

                string item = storage[0];
                storage.RemoveAt(0);
                Console.WriteLine($"[Consumer {consumerId}] Взяв елемент: {item}");

                Access.Release();
                Console.WriteLine($"[Consumer {consumerId}] Звільнено доступ до буфера.");

                Full.Release();
                Console.WriteLine($"[Consumer {consumerId}] Повідомлено, що звільнилось місце в буфері.");

                Console.WriteLine($"[Consumer {consumerId}] Обробляє елемент (300 мс)...");
                Thread.Sleep(300); 
            }

            Console.WriteLine($"[Consumer {consumerId}] Завершив обробку всіх елементів.");
        }

        // список випадкових чисел, які в сумі дорівнюють total, розміром parts
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