using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using ReactiveUI;
using System.Reactive;
using Task_2.Models;
using System.Collections.Generic;

namespace Task_4.ViewModels;

public class ReflectionViewModel : ReactiveObject
{
    private readonly Dictionary<Type, object> _instances = new();
    private string _assemblyPath;
    public string AssemblyPath
    {
        get => _assemblyPath;
        set => this.RaiseAndSetIfChanged(ref _assemblyPath, value);
    }

    private string _result = string.Empty;
    public string Result
    {
        get => _result;
        set => this.RaiseAndSetIfChanged(ref _result, value);
    }

    public ObservableCollection<Type> Classes { get; } = new();
    public ObservableCollection<MemberInfo> Members { get; } = new();

    public ReactiveCommand<Unit, Unit> LoadAssemblyCommand { get; }
    public ReactiveCommand<Unit, Unit> ExecuteMethodCommand { get; }

    private Type? _selectedClass;
    public Type? SelectedClass
    {
        get => _selectedClass;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedClass, value);
            if (value != null)
            {
                LoadMembers(value);
                // Показываем информацию о существующем экземпляре
                if (_instances.TryGetValue(value, out var instance))
                {
                    Result = $"Используется существующий экземпляр: {instance}";
                }
            }
        }
    }

    private MemberInfo? _selectedMember;
    public MemberInfo? SelectedMember
    {
        get => _selectedMember;
        set => this.RaiseAndSetIfChanged(ref _selectedMember, value);
    }

    private string _parameters;
    public string Parameters
    {
        get => _parameters;
        set => this.RaiseAndSetIfChanged(ref _parameters, value);
    }

    public ReflectionViewModel()
    {
        Console.WriteLine("ReflectionViewModel constructor это то должно работать");
        _parameters = string.Empty;
        LoadAssemblyCommand = ReactiveCommand.Create(LoadAssembly);
        ExecuteMethodCommand = ReactiveCommand.Create(() =>
        {
            if (SelectedClass != null && SelectedMember != null)
            {
                try
                {
                    if (SelectedMember is MethodInfo method)
                    {
                        var methodParams = method.GetParameters();
                        var inputParams = string.IsNullOrWhiteSpace(Parameters) ? new string[0] : Parameters.Split(',');
                        object[] convertedParams = new object[methodParams.Length];

                        // Получаем или создаем экземпляр класса
                        var instance = GetOrCreateInstance(SelectedClass);
                        if (instance == null)
                        {
                            Result = "Ошибка: Не удалось создать экземпляр класса";
                            return;
                        }

                        // Преобразуем параметры
                        for (int i = 0; i < methodParams.Length; i++)
                        {
                            var paramType = methodParams[i].ParameterType;
                            try
                            {
                                if (i < inputParams.Length)
                                {
                                    convertedParams[i] = ConvertParameter(inputParams[i].Trim(), paramType);
                                }
                                else
                                {
                                    convertedParams[i] = CreateDefaultValue(paramType);
                                }
                            }
                            catch (Exception ex)
                            {
                                Result = $"Ошибка преобразования параметра {i + 1}: {ex.Message}";
                                return;
                            }
                        }

                        // Выполняем метод
                        var result = method.Invoke(instance, convertedParams);
                        Result = $"Результат выполнения: {result ?? "void"}\nТекущий экземпляр: {instance}";
                    }
                    else if (SelectedMember is PropertyInfo property)
                    {
                        var instance = GetOrCreateInstance(SelectedClass);
                        if (instance == null)
                        {
                            Result = "Ошибка: Не удалось создать экземпляр класса";
                            return;
                        }

                        var value = property.GetValue(instance);
                        Result = $"Значение свойства: {value}\nТекущий экземпляр: {instance}";
                    }
                }
                catch (Exception ex)
                {
                    Result = $"Ошибка выполнения: {ex.Message}";
                }
            }
        });
    }

    private object? GetOrCreateInstance(Type type)
    {
        // Если экземпляр уже существует, возвращаем его
        if (_instances.TryGetValue(type, out var existingInstance))
        {
            return existingInstance;
        }

        // Создаем новый экземпляр
        var instance = CreateInstance(type);
        if (instance != null)
        {
            _instances[type] = instance;
        }
        return instance;
    }

    private object? CreateInstance(Type type)
    {
        try
        {
            // Для FileSystemItem и его наследников нужны специальные параметры
            if (typeof(FileSystemItem).IsAssignableFrom(type))
            {
                if (type == typeof(File))
                {
                    return new File("TestFile", null, 1024);
                }
                else if (type == typeof(Folder))
                {
                    return new Folder("TestFolder", null);
                }
            }
            
            // Для других типов используем конструктор по умолчанию
            return Activator.CreateInstance(type);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка создания экземпляра: {ex.Message}");
            return null;
        }
    }

    private object ConvertParameter(string value, Type targetType)
    {
        if (targetType == typeof(string))
            return value;
        if (targetType == typeof(int))
            return int.Parse(value);
        if (targetType == typeof(long))
            return long.Parse(value);
        if (targetType == typeof(bool))
            return bool.Parse(value);
        if (targetType == typeof(double))
            return double.Parse(value);
        if (targetType == typeof(Folder))
            return new Folder(value, null);
        if (targetType == typeof(File))
            return new File(value, null, 1024);
        
        return Convert.ChangeType(value, targetType);
    }

    private object? CreateDefaultValue(Type type)
    {
        if (type.IsValueType)
            return Activator.CreateInstance(type);
        return null;
    }

    public void LoadAssembly()
    {
        if (string.IsNullOrWhiteSpace(AssemblyPath)) return;

        try
        {
            Assembly assembly = Assembly.LoadFrom(AssemblyPath);
            var types = assembly.GetTypes().Where(t => typeof(FileSystemItem).IsAssignableFrom(t) && !t.IsAbstract);

            Classes.Clear();
            foreach (var type in types)
            {
                Console.WriteLine($"Найден класс: {type.FullName}");
                Classes.Add(type);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка загрузки сборки: {ex.Message}");
        }
    }

    public void LoadMembers(Type selectedType)
    {
        if (selectedType == null)
        {
            Console.WriteLine("LoadMembers: selectedType is null");
            return;
        }

        Console.WriteLine($"LoadMembers: Loading members for type {selectedType.FullName}");
        Members.Clear();
        
        // Получаем все публичные свойства
        var properties = selectedType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
        foreach (var property in properties)
        {
            Console.WriteLine($"LoadMembers: Found property {property.Name} from {property.DeclaringType?.Name}");
            Members.Add(property);
        }
        
        // Получаем все публичные методы
        var methods = selectedType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
            .Where(m => !m.IsSpecialName); // Исключаем методы свойств
            
        foreach (var method in methods)
        {
            Console.WriteLine($"LoadMembers: Found method {method.Name} from {method.DeclaringType?.Name}");
            Members.Add(method);
        }
        
        Console.WriteLine($"LoadMembers: Total members loaded: {Members.Count}");
    }

    public void ExecuteMethod(Type selectedType, MethodInfo method, object[] parameters)
    {
        var instance = Activator.CreateInstance(selectedType);
        method.Invoke(instance, parameters);
    }
} 