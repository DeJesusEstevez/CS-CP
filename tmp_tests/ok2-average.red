func average(nums: f[3]): f {
    declare sum: f = nums[0] + nums[1] + nums[2];
    gives sum / 3.0;
}

func testAverage(): f {
    declare values: f[3] = [1.0, 2.5, 3.5];
    gives average(values);
}
