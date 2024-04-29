using System.ComponentModel;
using ObservableCollections;
using MyR3Helpers;
using R3;

Person person1 = new("alice", 10);
Person person2 = new("bob", 20);

Console.WriteLine($"Init");
ObservableList<Person> people = [person1];

// コレクション内インスタンスの Age プロパティの変化を監視します
people.ObserveElementProperty(x => x.Age)
    .Subscribe(x =>
    {
        Console.WriteLine($"     Person: \"{x.Instance}\", {x.PropertyName}={x.Value}");
    });
Console.WriteLine($"  Count : {people.Count}\n");

Console.WriteLine($"Add");
people.Add(person2);
Console.WriteLine($"  Count : {people.Count}\n");

Console.WriteLine($"Change");
person1.Aging();
person2.Aging();
Console.WriteLine($"  Count : {people.Count}\n");

Console.WriteLine($"Remove");
people.Remove(person1);
Console.WriteLine($"  Count : {people.Count}\n");

Console.WriteLine($"Add");
people.Add(new("charlie", 30));
people[^1].Aging();
Console.WriteLine($"  Count : {people.Count}\n");

Console.WriteLine($"Replace");
people[0] = new("dave", 40);
Console.WriteLine($"  Count : {people.Count}\n");

Console.WriteLine($"Reset");
people.Clear();
Console.WriteLine($"  Count : {people.Count}\n");




class Person : INotifyPropertyChanged, IDisposable
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string Name { get; }
    public int Age
    {
        get => _age;
        private set
        {
            if (_age != value)
            {
                _age = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Age)));
            }
        }
    }
    private int _age;

    public Person(string name, int age) => (Name, Age) = (name, age);

    public void Aging() => Age++;

    public void Dispose() => Console.WriteLine($"Disposed : {this}");
    public override string ToString() => $"{Name}, {Age}";
}
