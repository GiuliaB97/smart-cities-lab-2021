package it.unibo.scl.es.result;

public class WekaClassifier {
	public static double classify(Object[] i)
    throws Exception {

    double p = Double.NaN;
    p = WekaClassifier.N5f41ab780(i);
    return p;
  }
  static double N5f41ab780(Object []i) {
    double p = Double.NaN;
    if (i[10] == null) {
      p = 0;
    } else if (((Double) i[10]).doubleValue() <= 0.6839229) {
    p = WekaClassifier.N506084231(i);
    } else if (((Double) i[10]).doubleValue() > 0.6839229) {
    p = WekaClassifier.N4c583f4c4(i);
    } 
    return p;
  }
  static double N506084231(Object []i) {
    double p = Double.NaN;
    if (i[16] == null) {
      p = 0;
    } else if (((Double) i[16]).doubleValue() <= 0.29620638) {
    p = WekaClassifier.N70833f0e2(i);
    } else if (((Double) i[16]).doubleValue() > 0.29620638) {
    p = WekaClassifier.N38a0e9d73(i);
    } 
    return p;
  }
  static double N70833f0e2(Object []i) {
    double p = Double.NaN;
    if (i[15] == null) {
      p = 0;
    } else if (((Double) i[15]).doubleValue() <= 1.0274897) {
      p = 0;
    } else if (((Double) i[15]).doubleValue() > 1.0274897) {
      p = 1;
    } 
    return p;
  }
  static double N38a0e9d73(Object []i) {
    double p = Double.NaN;
    if (i[9] == null) {
      p = 0;
    } else if (((Double) i[9]).doubleValue() <= 0.042600896) {
      p = 0;
    } else if (((Double) i[9]).doubleValue() > 0.042600896) {
      p = 1;
    } 
    return p;
  }
  static double N4c583f4c4(Object []i) {
    double p = Double.NaN;
    if (i[9] == null) {
      p = 2;
    } else if (((Double) i[9]).doubleValue() <= 5.711028) {
      p = 2;
    } else if (((Double) i[9]).doubleValue() > 5.711028) {
      p = 3;
    } 
    return p;
  }
}
