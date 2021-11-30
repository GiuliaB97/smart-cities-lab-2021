/** Classifier.java
 *  author: Andrea Cirri
 *  mail : andreacirri@gmail.com
 */
package it.unibo.scl.es.result;

import java.util.ArrayList;


public class Classifier {

	private ArrayList<Float> x;
	private ArrayList<Float> y;
	private ArrayList<Float> z;
	private Features features;
	private long startTime;

	public Classifier() {
		x = new ArrayList<Float>();
		y = new ArrayList<Float>();
		z = new ArrayList<Float>();
		features = new Features();
	}

	public String classify(float currX, float currY, float currZ) {
		String result = "";
		long currentTime = System.currentTimeMillis();
		if ((x.size() == 0) && (y.size() == 0) && (z.size() == 0)) {
			startTime = currentTime;
		}
		if (currentTime < startTime + 2000) {
			x.add(currX);
			y.add(currY);
			z.add(currZ);
		} else {
			features.calcolaX(x);
			features.calcolaY(y);
			features.calcolaZ(z);
			try {
				int res = (int) WekaClassifier.classify(features.getFeatures());
				if (res == 0)
					result = "staticoSulTavolo";
				else if (res == 1)
					result = "staticoInTasca";
				else if (res == 2)
					result = "camminando";
				else if (res == 3)
					result = "correndo";
				x.removeAll(x);
				y.removeAll(y);
				z.removeAll(z);
			} catch (Exception e) {
				e.printStackTrace();
			}
		}
		return result;
	}
}
