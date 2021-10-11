package it.unibo.scl.es.result;

import android.os.Bundle;
import android.app.Activity;
import android.content.Context;
import android.hardware.Sensor;
import android.hardware.SensorEvent;
import android.hardware.SensorEventListener;
import android.hardware.SensorManager;
import android.util.Log;
import android.view.View;
import android.view.animation.Animation;
import android.view.animation.AnimationUtils;
import android.widget.RelativeLayout;
import android.widget.TextView;
import android.widget.Toast;

public class MainActivity extends Activity implements SensorEventListener{
	
	private TextView tv_xValue;
	private TextView tv_yValue;
	private TextView tv_zValue;
	private TextView tv_onTableValue;
	private RelativeLayout rl_onTableRecognition;
	private TextView tv_actRecognitionValue;
	
	private SensorManager sensorManager;
	private Sensor sensor;
	
	private float last_x = 0.0f;
	private float last_y = 0.0f;
	private float last_z = 0.0f;
	private long lastUpdate = 0;
	private long currentUpdate = 0;	
	private static final int SHAKE_THRESHOLD = 2000;

	private Classifier classifier;


	@Override
	protected void onCreate(Bundle savedInstanceState) {
		super.onCreate(savedInstanceState);
		setContentView(R.layout.activity_main);
		
		tv_xValue = (TextView)findViewById(R.id.tv_x_value);
		tv_yValue = (TextView)findViewById(R.id.tv_y_value);
		tv_zValue = (TextView)findViewById(R.id.tv_z_value);
		tv_onTableValue = (TextView)findViewById(R.id.tv_on_table_recognition_value);
		rl_onTableRecognition = (RelativeLayout)findViewById(R.id.rl_on_table_recognition);
		tv_actRecognitionValue = (TextView)findViewById(R.id.tv_act_rec_value);
		
		sensorManager = (SensorManager) getSystemService(Context.SENSOR_SERVICE);
		sensor = sensorManager.getDefaultSensor(Sensor.TYPE_ACCELEROMETER); 

		classifier = new Classifier();
	}
	
	@Override
	protected void onResume() {
		super.onResume();

	}

	@Override
	protected void onPause() {
		sensorManager.unregisterListener(this);
		super.onPause();
	}
	
	@Override
	public void onAccuracyChanged(Sensor sensor, int accuracy) {
		
	}

	@Override
	public void onSensorChanged(SensorEvent event) {
		
		float x = event.values[0];
		float y = event.values[1];
		float z = event.values[2];

		//lettura su TextView
		setAccelerometerValues(x, y, z);
		
		//calcolo se il telefono è piano sul tavolo
		if((z >= 9.6f && z <= 10.4f) && (Math.abs(x) + Math.abs(y)) <= 0.5f){
			setOnTableRecognitionResult(true);
		}else{
			setOnTableRecognitionResult(false);
		}
		
		currentUpdate = System.currentTimeMillis();
		// only allow one update every 100ms.
	    if ((currentUpdate - lastUpdate) > 100) {
	      long diffTime = (currentUpdate - lastUpdate);
	      lastUpdate = currentUpdate;

	      float speed = Math.abs(x + y + z - last_x - last_y - last_z) / diffTime * 10000;

	      if (speed > SHAKE_THRESHOLD) {
	    	  shake();
	        //Log.d("sensor", "shake detected w/ speed: " + speed);
	        //Toast.makeText(this, "shake detected w/ speed: " + speed, Toast.LENGTH_SHORT).show();
	      }
	      
	      last_x = x;
	      last_y = y;
	      last_z = z;
	      
	    }
	    String res = classifier.classify(x, y, z);
	    if(!res.isEmpty()){
	    	setActivityRecognitionResult(res);
	    }
		
	}
	
	//callback onClick del button Start
	public void start(View view) {
		sensorManager.registerListener(this, sensor, SensorManager.SENSOR_DELAY_FASTEST);
	}
	
	//callback onClick del button Stop
	public void stop(View view) {
		sensorManager.unregisterListener(this);
	}

	
	public void setAccelerometerValues(float x, float y, float z){
		tv_xValue.setText(x+"");
		tv_yValue.setText(y+"");
		tv_zValue.setText(z+"");
	}
	
	public void setOnTableRecognitionResult(boolean result){
		if(result){
			tv_onTableValue.setText("On Table");
			rl_onTableRecognition.setBackgroundResource(R.drawable.ontable);
		}else{
			tv_onTableValue.setText("Not on Table");
			rl_onTableRecognition.setBackgroundResource(R.drawable.border);
		}
	}
	
	public void shake() {
		Animation shake = AnimationUtils.loadAnimation(this, R.anim.shake_animation);
		findViewById(R.id.tv_shake_value).startAnimation(shake);
	}
	
	public void setActivityRecognitionResult(String result){
		tv_actRecognitionValue.setText(result);
	}

}
