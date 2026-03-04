object Counter {
    declare value: i = 0;

    func inc(): i {
        set value = value + 1;
        gives value;
    }
}

func useCounter(): i {
    declare c: Counter = Counter();
    gives c.inc();
}
