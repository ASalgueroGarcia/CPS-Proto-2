using NUnit.Framework;

/// <summary>Independent music pitch and speed, and the glide used to change them.</summary>
public class AudioPitchTests
{
    [Test]
    public void Normal_speed_needs_a_shift_equal_to_the_pitch()
    {
        Assert.AreEqual(1f, AudioPitch.ShifterRatio(1f, 1f), 0.0001f);
        Assert.AreEqual(1.5f, AudioPitch.ShifterRatio(1.5f, 1f), 0.0001f);
    }

    [Test]
    public void Speed_change_is_cancelled_so_pitch_stays_put()
    {
        Assert.AreEqual(0.5f, AudioPitch.ShifterRatio(1f, 2f), 0.0001f);
        Assert.AreEqual(2f, AudioPitch.ShifterRatio(1f, 0.5f), 0.0001f);
        Assert.AreEqual(1f, AudioPitch.ShifterRatio(1.5f, 1.5f), 0.0001f);
    }

    [Test]
    public void Shift_stays_inside_what_the_mixer_effect_can_do()
    {
        Assert.AreEqual(AudioPitch.MaxShift, AudioPitch.ShifterRatio(2f, 0.5f), 0.0001f);
        Assert.AreEqual(AudioPitch.MinShift, AudioPitch.ShifterRatio(0.5f, 2f), 0.0001f);
        Assert.AreEqual(AudioPitch.MaxShift, AudioPitch.ShifterRatio(1f, 0f), 0.0001f);
    }

    [Test]
    public void Tween_arrives_in_the_given_time()
    {
        var value = new TweenedValue(1f);
        value.MoveTo(2f, 0.5f);

        Assert.IsTrue(value.Step(0.25f));
        Assert.AreEqual(1.5f, value.Current, 0.0001f);
        Assert.IsTrue(value.Step(0.25f));
        Assert.AreEqual(2f, value.Current, 0.0001f);
        Assert.IsFalse(value.Step(0.25f));
    }

    [Test]
    public void Tween_with_no_time_jumps_on_the_next_step()
    {
        var value = new TweenedValue(0f);
        value.MoveTo(0.25f, 0f);

        Assert.AreEqual(0f, value.Current);
        Assert.IsTrue(value.Step(0f));
        Assert.AreEqual(0.25f, value.Current);
    }

    [Test]
    public void Tween_can_be_redirected_midway()
    {
        var value = new TweenedValue(0f);
        value.MoveTo(1f, 1f);
        value.Step(0.5f);
        value.MoveTo(0f, 0.5f);
        value.Step(0.5f);

        Assert.AreEqual(0f, value.Current, 0.0001f);
    }

    [Test]
    public void Filter_is_open_at_zero_and_fully_closed_at_one()
    {
        Assert.AreEqual(22000f, AudioFilter.Cutoff(22000f, 1800f, 0f), 0.01f);
        Assert.AreEqual(1800f, AudioFilter.Cutoff(22000f, 1800f, 1f), 0.01f);
        Assert.AreEqual(450f, AudioFilter.Cutoff(10f, 450f, 1f), 0.01f);
    }

    [Test]
    public void Filter_sweeps_in_equal_pitch_steps()
    {
        Assert.AreEqual(2000f, AudioFilter.Cutoff(20000f, 200f, 0.5f), 0.5f);
    }
}
