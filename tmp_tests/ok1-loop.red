entry func main(): i {
    declare total: i = 0;
    loop (declare idx: i = 0; idx < 5; set idx = idx + 1; ) {
        set total = total + idx;
    }
    check (total > 5) { set total = total * 2; }
    gives total;
}
