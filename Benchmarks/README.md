# Orleans MongoDB Benchmarks

Executes various synthetic benches to determine implementation quality.

```shell
# test the default Reminder storage with a load factor of 2
dotnet run --configuration Release -- --reminder-strategy=StandardStorage -c 10
# 

# test the new Reminder HashedLookupStorage with a load factor of 10
dotnet run --configuration Release -- --reminder-strategy=HashedLookupStorage -c 10
# and stress it !!!
dotnet run --configuration Release -- --reminder-strategy=HashedLookupStorage -c 50
```