using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Collections.Generic
{
    namespace System.Linq
    {
        class Program
        {
            static void Main(string[] args)
            {
                
                List<Student> students = new List<Student>
            {
            new Student() { LastName = "Doe", FirstName = "Jhon", Age = 25 },
            new Student() { LastName = "Danil", FirstName = "Jane", Age = 27 },
            new Student() { LastName = "Parker", FirstName = "Sara", Age = 21 },
            new Student() { LastName = "Simpson", FirstName = "Jessica", Age = 21 },
            new Student() { LastName = "Washington", FirstName = "Andre", Age = 21 }
            };

                List<Teacher> teachers = new List<Teacher>
            {
            new Teacher() { LastName = "Williams", FirstName = "Michael", Age = 33 },
            new Teacher() { LastName = "Anderson", FirstName = "Robert", Age = 41 },
            new Teacher() { LastName = "Wilson", FirstName = "William", Age = 44 },
            new Teacher() { LastName = "Harris", FirstName = "Richard", Age = 54 },
            new Teacher() { LastName = "Clark", FirstName = "Thomas", Age = 48 }
            };

                //Count
                //Returns the number of elements in a sequence.
                Print("Count");
                int countS = students._Count(st => st.Age == 21);
                int countT = teachers._Count(tch => tch.Age < 40);
                Console.WriteLine($"Students {countS}, Teachers {countT}");

                //GroupBy
                Print("GroupBy");
                var query1 = students._GroupBy(student => student.Age == 21);
                var query2 = students._GroupBy(student => student.Age > 24);
                foreach (var item in query1)
                {
                    Console.Write($"{item._Count()}, ");
                }
                Console.WriteLine();
                foreach (var item in query2)
                {
                    Console.Write($"{item._Count()}, ");
                }
                Console.WriteLine();

                //FirstOrDefault, First
                Print("FirstOrDefault, First");
                var res1 = students._FirstOrDefault(st => st.Age > 25); //return null
                var res2 = students._First(st => st.Age > 25); //throw an Exception
                Console.WriteLine($"{res1.Age} {res2.Age}");

                //Aggregate
                Print("Aggregate");
                var olderAge = students._Aggregate((older, next) => next.Age > older.Age ? next : older);
                Console.WriteLine($"{olderAge.FirstName} {olderAge.Age}");

                //All
                Print("All");
                var isAdult = students._All(st => st.Age > 18);
                Console.WriteLine($"{isAdult}");

                //Avarage
                Print("Avarage");
                var avarageAge = students._Average(age => age.Age);
                Console.WriteLine($"{avarageAge}");

                //Select, Concat
                Print("Select, Concat");
                var query = students._Select(st => st.LastName)._Concat(teachers._Select(tch => tch.LastName));
                foreach (string name in query)
                {
                    Console.Write($"{name}, ");
                }
                Console.WriteLine();

                //Where
                Print("Where");
                List<string> fruits = new List<string> 
                                    { "apple", "passionfruit", "banana", "mango",
                                    "orange", "blueberry", "grape", "strawberry" };
                IEnumerable<string> query0 = fruits._Where(fruit => fruit.Length < 6);
                foreach (string fruit in query0)
                {
                    Console.WriteLine(fruit);
                }

                //Distinct
                //Returns distinct elements from a sequence.
                Print("Distinct");
                IEnumerable<string> distinctLastNames = query._Distinct();
                Console.WriteLine("\nDistinct LastNames:");
                foreach (string lastName in distinctLastNames)
                {
                    Console.WriteLine(lastName);
                }

                //Any
                Print("Any");
                bool hasElements = teachers._Any();
                Console.WriteLine("The list {0} empty.",
                    hasElements ? "is not" : "is");

                //AsEnumerable
                Print("AsEnumerable");
                string[] array = new string[] { "Yerevan", "Gyumri", "Vanadzor", "Vagharshapat", "Abovyan", "Kapan", "Hrazdan", "Artashat", "Armavir", "Dilijan" };
                var query3 = array._AsEnumerable()._Where(str => str.Contains("A"));
                foreach (var ele in query3)
                {
                    Console.WriteLine(ele);
                }

                //Cast, OrderBy, Select
                Print("Cast, OrderBy, Select");
                ArrayList arrayList = new ArrayList() { "Yerevan", "Gyumri", "Vanadzor", "Vagharshapat", "Abovyan", "Kapan", "Hrazdan", "Artashat", "Armavir", "Dilijan" };
                IEnumerable<string> query4 = arrayList._Cast<string>()._OrderBy(city => city)._Select(city => city);
                foreach (string city in query4)
                {
                    Console.WriteLine(city);
                }
                // The following code, without the cast, doesn't compile.
                //IEnumerable<string> query5 = arrayList.OrderBy(city => city).Select(city => city);

                //Contains
                Print("Contains");
                var city1 = arrayList[4];
                if (array._Contains(city1))
                {
                    Console.WriteLine(city1);
                }

                //DefaultIfEmpty
                Print("DefaultIfEmpty");
                List<string> emptyList = new List<string>();
                List<int> numList = new List<int> { 1, 3, 5, 7, 9 };
                var res3 = emptyList._DefaultIfEmpty();
                Console.WriteLine(res3);
                var res4 = numList._DefaultIfEmpty();
                Console.WriteLine(res4);

                //ElementAt
                Print("ElementAt");
                var teachers1 = teachers._ElementAt(1);
                Console.WriteLine(teachers1.FirstName);
                //ElementAtOrDefault
                Print("ElementAtOrDefault");
                var teachers2 = teachers._ElementAtOrDefault(7);
                Console.WriteLine($"{0}",  teachers2 !=null ? teachers2.FirstName : null);


                //TODO:
                //Except
                //groupJoin
                //Intersect
                //Join
                //Last
                //LastOrDefault
                //LongCount
                //Max
                //Min
                //OfType
                //OrderBy
                //OrderByDescending
                //Reverse
                //SelectMany
                //SequenceEqual
                //Single
                //SingleOrDefault
                //Skip
                //SkipWhile
                //Sum
                //Take
                //TakeWhile
                //ToArray
                //ToDictionary
                //ToHashSet
                //ToList
                //ToLookup
                //Union
                //Zip
                
                Console.ReadKey();
            }
            public static void Print(string title)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write(new string('*', 10));
                Console.Write(title);
                Console.Write(new string('*', 10));
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine();
            }

        }
    }
    public class Student
    {
        public Student()
        {
            Id = Guid.NewGuid();
        }
        public Guid Id { get; set; }
        public int Age { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
    }
    public class Teacher
    {
        public Teacher()
        {
            Id = Guid.NewGuid();
        }
        public Guid Id { get; set; }
        public int Age { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
    }
}