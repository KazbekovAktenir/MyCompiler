public class PseudocodeTranslator {
    public static void main(String[] args) {
        int x = 10;
        int y = 20;
        if (x < y) {
            System.out.println("x меньше y");
        } else {
            System.out.println("x не меньше y");
        }
        for (int i = 1; i <= 5; i++) {
            System.out.println("Итерация: " + i);
        }
        System.out.println("Сумма: " + сумма ( x , y ));
    }
    public static int сумма(int a, int b) {
        var результат = a + b;
        return результат;
    }

}
