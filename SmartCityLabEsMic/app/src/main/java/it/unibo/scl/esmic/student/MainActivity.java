package it.unibo.scl.esmic.student;

import java.util.ArrayList;
import java.util.List;

import android.Manifest;
import android.app.Activity;
import android.content.Intent;
import android.content.pm.PackageManager;
import android.content.pm.ResolveInfo;
import android.media.AudioFormat;
import android.media.AudioRecord;
import android.media.MediaRecorder.AudioSource;
import android.os.Bundle;
import android.speech.RecognizerIntent;
import androidx.core.app.ActivityCompat;
import androidx.core.content.ContextCompat;
import android.view.View;
import android.widget.TextView;
import android.widget.Toast;

public class MainActivity extends Activity {
	private final static double EAR_THRESHOLD = 0.00002;
	private final static int CHANNEL_CONFIGURATION = AudioFormat.CHANNEL_IN_DEFAULT;
	private final static int ENCODING = AudioFormat.ENCODING_PCM_16BIT;
	private final static int SAMPLE_RATE = 44100;
	private final static double RMS_THRESHOLD = 60;
	private final static double  MIC_CALIBRATION = 100;
	private final static int REQUEST_CODE = 1234;

	private AudioRecord recorder = null;
	private int bufferSize = 0;
	public boolean isRecording = false;
	private short[] audioData = null;
	private Thread recordingThread;

	TextView rmsValue;
	TextView rmsResult;
	TextView activityRecTextView;
	TextView activityRecHintsTextView;
	View rlRms;

	@Override
	protected void onCreate(Bundle savedInstanceState) {
		super.onCreate(savedInstanceState);
		setContentView(R.layout.activity_main);
		rmsValue = (TextView) findViewById(R.id.tv_rms_value);
		rmsResult = (TextView) findViewById(R.id.tv_audio_value);
		activityRecTextView = (TextView) findViewById(R.id.tv_voice_recognition_main_value);
		activityRecHintsTextView = (TextView) findViewById(R.id.tv_voice_recognition_other_values);
		rlRms = findViewById(R.id.rl_mic_values);
		((TextView) findViewById(R.id.tv_threshold_rms_value)).setText(RMS_THRESHOLD + "");

		requestRecordAudioPermission();
	}

	@Override
	protected void onPause() {
		stop(null);
		super.onPause();
	}


	private void requestRecordAudioPermission() {
		//controllo la versione delle API, se inferiore a 23 non faccio nulla
		int currentapiVersion = android.os.Build.VERSION.SDK_INT;
		if (currentapiVersion > android.os.Build.VERSION_CODES.LOLLIPOP){

			if (ContextCompat.checkSelfPermission(this, Manifest.permission.RECORD_AUDIO) != PackageManager.PERMISSION_GRANTED) {

				if (ActivityCompat.shouldShowRequestPermissionRationale(this, Manifest.permission.RECORD_AUDIO)) {
					//eventuale messaggio di errore
				}
				else {
					ActivityCompat.requestPermissions(this, new String[]{Manifest.permission.RECORD_AUDIO}, 1);
				}
			}
		}
	}


	public void start(View v) {

		bufferSize = AudioRecord.getMinBufferSize(SAMPLE_RATE, CHANNEL_CONFIGURATION, ENCODING);

		recorder = new AudioRecord(AudioSource.MIC, SAMPLE_RATE, CHANNEL_CONFIGURATION, ENCODING, bufferSize);

		recorder.startRecording();

		isRecording = true;

		audioData = new short[bufferSize];

		recordingThread = new Thread(()->{ processRMS();}, "Audio RMS Thread");

		recordingThread.start();
	}

	public void stop(View v) {
		if (recorder != null) {
			isRecording = false;
			recorder.stop();
			recorder.release();
			recorder = null;
			recordingThread = null;
		}
	}

	public void startVoiceRecognition(View v) {
		PackageManager pm = getPackageManager();
		List<ResolveInfo> activities = pm.queryIntentActivities(new Intent(RecognizerIntent.ACTION_RECOGNIZE_SPEECH), 0);
		if (activities.size() == 0)
		{
			Toast.makeText(getApplicationContext(), "Libreria per il riconoscimento vocale non disponibile.", Toast.LENGTH_LONG).show();
			return;
		}else{
			Intent intent = new Intent(RecognizerIntent.ACTION_RECOGNIZE_SPEECH);
			intent.putExtra(RecognizerIntent.EXTRA_LANGUAGE_MODEL, RecognizerIntent.LANGUAGE_MODEL_FREE_FORM);
			intent.putExtra(RecognizerIntent.EXTRA_PROMPT, "Test di Activity Recognition");
			startActivityForResult(intent, REQUEST_CODE);
		}
	}

	private void processRMS() {
		double rmsdB;
		double rms;
		while (isRecording) {

			recorder.read(audioData, 0, bufferSize);

			rms = 0;

			for (int i = 0; i < audioData.length; i++) {
				rms += audioData[i] * audioData[i];
			}

			rms = Math.sqrt(rms / audioData.length);

			rmsdB = spl(rms);

			setRMSValue(rmsdB - MIC_CALIBRATION);
		}

	}

	private double spl(double rms){
		return 20 * Math.log10(rms / EAR_THRESHOLD);
	}

	@Override
	protected void onActivityResult(int requestCode, int resultCode, Intent data) {
		if (requestCode == REQUEST_CODE && resultCode == RESULT_OK) {
			ArrayList<String> matches = data
					.getStringArrayListExtra(RecognizerIntent.EXTRA_RESULTS);
			setActivityRecognitionResult(matches);
		}
		super.onActivityResult(requestCode, resultCode, data);
	}

	public void setActivityRecognitionResult(List<String> list){
		activityRecTextView.setText(list.get(0));
		list.remove(0);
		String hints = "";
		for (String result : list) {
			hints += result + ", ";
		}
		if (hints.equals("")) {
			activityRecHintsTextView.setText("");
		}
		else{
			activityRecHintsTextView.setText(hints.substring(0, hints.length()-2));
		}
	}

	public void setRMSValue(final double value) {
		runOnUiThread(new Runnable() {
			@Override
			public void run() {
				rmsValue.setText(value + "");
				if (value >= RMS_THRESHOLD) {
					rmsResult.setText("Rumore");
					rlRms.setBackgroundResource(R.drawable.onnoise);
				} else {
					rmsResult.setText("Silenzio");
					rlRms.setBackgroundResource(R.drawable.border);
				}

			}
		});
	}

}
