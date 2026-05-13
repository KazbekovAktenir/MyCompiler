def сумма(a, b):
    результат = a + b
    return результат
x = 10
y = 20
if x < y:
    print("x меньше y")
else:
    print("x не меньше y")
for i in range(1, 6):
    print("Итерация: " + str(i))
print("Сумма: " + str(сумма(x, y)))
